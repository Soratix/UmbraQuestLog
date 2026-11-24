using System;
using System.Collections.Generic;
using System.Linq;

namespace Umbra.QuestLog;

/// <summary>Ce qu'il faut savoir du personnage pour appliquer les règles d'exclusion.</summary>
/// <param name="IsComplete">Quête terminée (QuestManager.IsQuestComplete dans le jeu).</param>
/// <param name="GrandCompany">Grande Compagnie actuelle (0 = aucune).</param>
/// <param name="HideLocked">Masquer les quêtes des extensions pas encore atteintes dans le scénario.</param>
public sealed record PlayerContext(Func<uint, bool> IsComplete, uint GrandCompany, bool HideLocked);

public sealed record CategoryProgress(QuestCategory Category, int Done, int Total, List<QuestEntry> Remaining)
{
    public float Ratio => Total == 0 ? 1f : Done / (float)Total;
}

public sealed record SectionProgress(QuestSection Section, int Done, int Total, List<CategoryProgress> Categories)
{
    public float Ratio => Total == 0 ? 1f : Done / (float)Total;
}

/// <summary>
/// Totaux de complétion d'un personnage.
///
/// Exclusions qui dépendent du personnage (une quête faite est toujours comptée, sauf mention) :
///  1. quête retirée du jeu et pas faite ;
///  2. quête réservée à une autre cité de départ (faite ou non : elle ne peut pas l'être) ;
///  3. quête de classe prévue pour ceux qui prennent la classe plus tard, quand c'est la classe de départ ;
///  4. QuestLock : verrouillée par une quête déjà faite (choix exclusifs, Grande Compagnie choisie…) ;
///  5. Quest.GrandCompany différente de la Grande Compagnie actuelle (ou pas encore de Grande Compagnie) ;
///  6. option : extension pas encore atteinte dans le scénario principal.
/// </summary>
public sealed class QuestProgress
{
    public List<SectionProgress>               Sections   { get; } = [];
    public Dictionary<uint, CategoryProgress>  Categories { get; } = [];

    public int Done  { get; private set; }
    public int Total { get; private set; }

    public float Ratio => Total == 0 ? 1f : Done / (float)Total;

    public static StartCity PlayerStartCity(Func<uint, bool> isComplete)
        => isComplete(QuestLists.ComingToGridania) ? StartCity.Gridania
         : isComplete(QuestLists.ComingToLimsa) ? StartCity.Limsa
         : isComplete(QuestLists.ComingToUldah) ? StartCity.Uldah
         : StartCity.None;

    public static QuestProgress Evaluate(QuestCatalog catalog, PlayerContext player)
    {
        var  isComplete = player.IsComplete;
        var  city       = PlayerStartCity(isComplete);

        HashSet<uint> startClassExcluded = [];
        foreach (var (start, excluded) in QuestLists.StartClass) {
            if (isComplete(start) && !isComplete(excluded[0])) startClassExcluded.UnionWith(excluded);
        }

        Dictionary<uint, bool> reached = [];
        bool ExpansionReached(uint expansion)
        {
            if (expansion == 0) return true;
            if (reached.TryGetValue(expansion, out bool r)) return r;

            r = !catalog.ExpansionGates.TryGetValue(expansion, out var gates) || gates.Any(isComplete);
            if (!r && catalog.MainScenarioByExpansion.TryGetValue(expansion, out var msq)) r = msq.Any(isComplete);

            reached[expansion] = r;
            return r;
        }

        var progress = new QuestProgress();

        foreach (var section in catalog.Sections) {
            List<CategoryProgress> categories = [];
            int sectionDone = 0, sectionTotal = 0;

            foreach (var category in section.Categories) {
                int done = 0, total = 0;
                List<QuestEntry> remaining = [];

                foreach (var q in category.Quests) {
                    bool complete = q.Ids.Any(isComplete);

                    if (!complete) {
                        if (q.Retired) continue;
                        if (q.Ids.Any(startClassExcluded.Contains)) continue;
                        if (q.Locks.Any(isComplete)) continue;
                        if (q.GrandCompany != 0 && q.GrandCompany != player.GrandCompany) continue;
                        if (player.HideLocked && !ExpansionReached(q.Expansion)) continue;
                    }

                    if (q.StartCity != StartCity.None && city != StartCity.None && (q.StartCity & city) == 0) continue;

                    total++;
                    if (complete) done++;
                    else remaining.Add(q);
                }

                if (total == 0) continue;

                var cp = new CategoryProgress(category, done, total, remaining);
                categories.Add(cp);
                progress.Categories[category.Id] = cp;
                sectionDone  += done;
                sectionTotal += total;
            }

            if (sectionTotal == 0) continue;

            progress.Sections.Add(new SectionProgress(section, sectionDone, sectionTotal, categories));
            progress.Done  += sectionDone;
            progress.Total += sectionTotal;
        }

        return progress;
    }
}
