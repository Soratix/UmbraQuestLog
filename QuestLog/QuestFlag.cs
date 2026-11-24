using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MapRow = Lumina.Excel.Sheets.Map;

namespace Umbra.QuestLog;

/// <summary>
/// Clic sur une quête : drapeau sur la carte à l'emplacement du PNJ de départ (Quest.IssuerLocation,
/// une ligne de la feuille Level), et lien de carte dans le chat.
/// </summary>
internal static unsafe class QuestFlag
{
    public static void Place(QuestEntry quest, bool openMap)
    {
        var chat = Framework.Service<IChatGui>();

        if (!quest.HasLocation) {
            chat.PrintError($"{quest.Name} — {Strings.T("Chat.NoLocation")}", Strings.T("WidgetName"));
            return;
        }

        var agent = AgentMap.Instance();
        if (agent == null) return;

        agent->SetFlagMapMarker(quest.TerritoryId, quest.MapId, quest.Position);
        if (openMap) agent->OpenMapByMapId(quest.MapId, quest.TerritoryId);

        string who = string.IsNullOrEmpty(quest.IssuerName) ? quest.ZoneName : $"{quest.IssuerName}, {quest.ZoneName}";
        var message = new SeStringBuilder().AddText(Strings.T("Chat.Flag", quest.Name, who));

        // Le lien de carte attend des coordonnées « lisibles » (11,5 ; 8,2), pas des coordonnées du monde.
        if (Framework.Service<IDataManager>().GetExcelSheet<MapRow>().GetRowOrDefault(quest.MapId) is { } map) {
            Vector2 coords = MapUtil.WorldToMap(new Vector2(quest.Position.X, quest.Position.Z), map);
            message.AddText(Strings.T("Chat.Sep")).Append(SeString.CreateMapLink(quest.TerritoryId, quest.MapId, coords.X, coords.Y, 0f));
        }

        chat.Print(message.Build(), Strings.T("WidgetName"));
    }
}
