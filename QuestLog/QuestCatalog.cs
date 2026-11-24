using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Lumina.Excel;
using ENpcResidentRow = Lumina.Excel.Sheets.ENpcResident;
using JournalCategoryRow = Lumina.Excel.Sheets.JournalCategory;
using JournalSectionRow = Lumina.Excel.Sheets.JournalSection;
using QuestRow = Lumina.Excel.Sheets.Quest;

namespace Umbra.QuestLog;

/// <summary>Une quête du journal, telle qu'elle est comptée (une entrée peut regrouper plusieurs ID).</summary>
public sealed record QuestEntry(
    uint     Id,
    uint[]   Ids,
    string   Name,
    int      Level,
    uint     GenreId,
    ushort   SortKey,
    uint     Expansion,
    uint     GrandCompany,
    StartCity StartCity,
    bool     Retired,
    uint     IssuerNpcId,
    string   IssuerName,
    uint     TerritoryId,
    uint     MapId,
    Vector3  Position,
    string   ZoneName
)
{
    public bool HasLocation => TerritoryId != 0 && MapId != 0;
}

/// <summary>Verrous et prérequis d'une quête (feuille Quest).</summary>
/// <param name="Locks">QuestLock : la quête devient impossible dès que l'une de celles-ci est faite.</param>
/// <param name="Previous">PreviousQuest : quêtes à faire avant.</param>
/// <param name="AnyPrevious">PreviousQuestJoin = 2 : une seule suffit (sinon toutes sont requises).</param>
public sealed record QuestRequirement(uint[] Locks, uint[] Previous, bool AnyPrevious);

/// <summary>Catégorie du journal (JournalCategory) : « Quêtes annexes de Gyr Abania », « Quêtes de rôle »…</summary>
public sealed class QuestCategory(uint id, string name, QuestSection section)
{
    public uint             Id      { get; } = id;
    public string           Name    { get; } = name;
    public QuestSection     Section { get; } = section;
    public List<QuestEntry> Quests  { get; } = [];
}

/// <summary>
/// Section du journal (JournalSection). Les sections dont le nom ne diffère que par une précision
/// entre parenthèses (« Scénario principal (A Realm Reborn à Endwalker) » / « (Dawntrail) ») sont fusionnées.
/// </summary>
public sealed class QuestSection(uint id, string name)
{
    public uint                Id         { get; } = id;
    public string              Name       { get; } = name;
    public List<QuestCategory> Categories { get; } = [];
}

/// <summary>Cité de départ à laquelle une quête est réservée (identifiants de QuestTracker).</summary>
[Flags]
public enum StartCity : byte
{
    None     = 0,
    Gridania = 1,
    Limsa    = 2,
    Uldah    = 4,
}

/// <summary>
/// Toutes les quêtes du journal, construites une fois à partir des feuilles du jeu (langue du client).
///
/// Hiérarchie : Quest.JournalGenre → JournalGenre.JournalCategory → JournalCategory.JournalSection.
///
/// Exclusions statiques (ne dépendent pas du personnage) :
///  - quêtes sans nom ;
///  - JournalGenre 0 : quasi-quêtes et missions qui n'apparaissent pas dans le journal ;
///  - Festival ≠ 0 : quêtes d'événements saisonniers, impossibles hors événement ;
///  - catégories sans section (hors journal) ;
///  - doublons du scénario de A Realm Reborn : la même quête existe sous plusieurs ID
///    (une par cité, ou une version « retirée »). Ils sont regroupés en une seule entrée,
///    faite si l'un des ID est fait (listes reprises de QuestTracker, keifufu, AGPL-3.0).
/// </summary>
public sealed class QuestCatalog
{
    public List<QuestSection> Sections { get; } = [];

    public IEnumerable<QuestCategory> Categories => Sections.SelectMany(s => s.Categories);

    public IEnumerable<QuestEntry> Quests => Categories.SelectMany(c => c.Quests);

    /// <summary>
    /// Extension → quêtes du scénario principal dont l'une doit être faite pour l'avoir atteinte
    /// (la dernière quête de l'extension précédente, prérequis de la première de celle-ci).
    /// </summary>
    public Dictionary<uint, HashSet<uint>> ExpansionGates { get; } = [];

    /// <summary>Quêtes du scénario principal, par extension (une seule faite suffit à prouver qu'on l'a atteinte).</summary>
    public Dictionary<uint, List<uint>> MainScenarioByExpansion { get; } = [];

    /// <summary>
    /// Toutes les quêtes du jeu (journal ou non) qui ont un verrou ou un prérequis :
    /// QuestLock (verrouillée si l'une est faite) et PreviousQuest
    /// (PreviousQuestJoin 1 = toutes requises, 2 = une seule suffit).
    /// </summary>
    public Dictionary<uint, QuestRequirement> Requirements { get; } = [];

    private static readonly Regex Parenthesis = new(@"\s*[\(（][^\)）]*[\)）]", RegexOptions.Compiled);

    public static string TrimSectionName(string name) => Parenthesis.Replace(name, string.Empty).Trim();

    public static QuestCatalog Build(ExcelModule excel)
    {
        var catalog    = new QuestCatalog();
        var quests     = excel.GetSheet<QuestRow>();
        var residents  = excel.GetSheet<ENpcResidentRow>();
        var categories = excel.GetSheet<JournalCategoryRow>();
        var sections   = excel.GetSheet<JournalSectionRow>();

        // Sections, fusionnées par nom sans parenthèses, dans l'ordre du jeu.
        Dictionary<uint, QuestSection> sectionById   = [];
        Dictionary<string, QuestSection> sectionByName = [];
        foreach (var s in sections) {
            string name = TrimSectionName(s.Name.ExtractText());
            if (name.Length == 0) continue;

            if (!sectionByName.TryGetValue(name, out var section)) {
                section             = new QuestSection(s.RowId, name);
                sectionByName[name] = section;
                catalog.Sections.Add(section);
            }

            sectionById[s.RowId] = section;
        }

        Dictionary<uint, QuestCategory> categoryById = [];
        foreach (var c in categories) {
            if (c.RowId == 0 || !sectionById.TryGetValue(c.JournalSection.RowId, out var section)) continue;

            var category = new QuestCategory(c.RowId, c.Name.ExtractText(), section);
            categoryById[c.RowId] = category;
            section.Categories.Add(category);
        }

        // Doublons du scénario de ARR : ID → groupe.
        Dictionary<uint, uint[]> groupOf = [];
        foreach (var group in QuestLists.DuplicateGroups) {
            foreach (uint id in group) groupOf[id] = group;
        }

        HashSet<uint> grouped = [];
        HashSet<uint> mainScenarioSections = [];

        foreach (var q in quests) {
            uint[] locks    = q.QuestLock.Select(l => l.RowId).Where(id => id != 0).ToArray();
            uint[] previous = q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).ToArray();
            if (locks.Length > 0 || previous.Length > 0) {
                catalog.Requirements[q.RowId] = new QuestRequirement(locks, previous, q.PreviousQuestJoin == 2);
            }
        }

        foreach (var q in quests) {
            if (q.Name.IsEmpty || q.JournalGenre.RowId == 0 || q.Festival.RowId != 0) continue;

            var genre = q.JournalGenre.ValueNullable;
            if (genre == null || !categoryById.TryGetValue(genre.Value.JournalCategory.RowId, out var category)) continue;

            string name = q.Name.ExtractText().Trim();
            if (name.Length == 0) continue;

            uint[] ids = [q.RowId];
            if (q.Expansion.RowId == 0 && groupOf.TryGetValue(q.RowId, out var group)) {
                if (grouped.Contains(q.RowId)) continue;
                ids = group;
                grouped.UnionWith(group);
            }

            uint    npcId     = q.IssuerStart.RowId;
            string  npcName   = residents.GetRowOrDefault(npcId)?.Singular.ExtractText() ?? string.Empty;
            uint    territory = 0, map = 0;
            Vector3 position  = default;
            string  zone      = q.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;

            if (q.IssuerLocation.ValueNullable is { } level && level.Territory.RowId != 0) {
                territory = level.Territory.RowId;
                map       = level.Map.RowId;
                position  = new Vector3(level.X, level.Y, level.Z);

                string levelZone = level.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty;
                if (levelZone.Length > 0) zone = levelZone;
            }

            var city = StartCity.None;
            if (QuestLists.GridaniaStart.Contains(q.RowId)) city |= StartCity.Gridania;
            if (QuestLists.LimsaStart.Contains(q.RowId)) city |= StartCity.Limsa;
            if (QuestLists.UldahStart.Contains(q.RowId)) city |= StartCity.Uldah;
            if (q.RowId == QuestLists.CallOfTheSea) city = StartCity.Gridania | StartCity.Limsa;

            category.Quests.Add(new QuestEntry(
                q.RowId,
                ids,
                name,
                q.ClassJobLevel[0],
                genre.Value.RowId,
                q.SortKey,
                q.Expansion.RowId,
                q.GrandCompany.RowId,
                city,
                ids.Any(QuestLists.Retired.Contains),
                npcId,
                npcName,
                territory,
                map,
                position,
                zone
            ));

            // Scénario principal : la section 0 et celles fusionnées avec elle (Dawntrail).
            if (category.Section.Id == MainScenarioSectionId) {
                mainScenarioSections.Add(q.RowId);
                if (!catalog.MainScenarioByExpansion.TryGetValue(q.Expansion.RowId, out var list)) {
                    catalog.MainScenarioByExpansion[q.Expansion.RowId] = list = [];
                }
                list.Add(q.RowId);
            }
        }

        // Portes d'extension : une quête du scénario dont un prérequis appartient à une extension antérieure.
        foreach (uint id in mainScenarioSections) {
            var q = quests.GetRow(id);
            foreach (var prev in q.PreviousQuest) {
                if (prev.RowId == 0) continue;
                var p = quests.GetRowOrDefault(prev.RowId);
                if (p == null || p.Value.Expansion.RowId >= q.Expansion.RowId) continue;

                if (!catalog.ExpansionGates.TryGetValue(q.Expansion.RowId, out var gates)) {
                    catalog.ExpansionGates[q.Expansion.RowId] = gates = [];
                }
                gates.Add(prev.RowId);
            }
        }

        foreach (var category in catalog.Categories) {
            category.Quests.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level)
                : a.GenreId != b.GenreId ? a.GenreId.CompareTo(b.GenreId)
                : a.SortKey != b.SortKey ? a.SortKey.CompareTo(b.SortKey)
                : a.Id.CompareTo(b.Id));
        }

        foreach (var section in catalog.Sections) section.Categories.RemoveAll(c => c.Quests.Count == 0);
        catalog.Sections.RemoveAll(s => s.Categories.Count == 0);

        return catalog;
    }

    /// <summary>Section « Scénario principal » (JournalSection 0, fusionnée avec celle de Dawntrail).</summary>
    public const uint MainScenarioSectionId = 0;
}
