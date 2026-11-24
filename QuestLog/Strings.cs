using Dalamud.Game;

namespace Umbra.QuestLog;

/// <summary>
/// Textes du widget en français et en anglais.
/// Umbra ne permet pas aux widgets tiers d'ajouter leurs clés à sa propre table I18N :
/// on suit la langue d'Umbra (I18N.GetCurrentLanguage : réglage d'Umbra, sinon langue de
/// l'interface Dalamud), et à défaut celle du client du jeu. Français si « fr », anglais sinon.
/// Les noms qui viennent du jeu (quêtes, PNJ, zones, catégories du journal) restent dans la langue du client.
/// </summary>
internal static class Strings
{
    public static bool IsFrench
    {
        get {
            try {
                string lang = I18N.GetCurrentLanguage();
                if (!string.IsNullOrEmpty(lang)) return lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            } catch {
                // I18N indisponible : on se rabat sur la langue du client.
            }

            try {
                return Framework.Service<IClientState>().ClientLanguage == ClientLanguage.French;
            } catch {
                return false;
            }
        }
    }

    private static readonly Dictionary<string, (string Fr, string En)> Table = new() {
        ["WidgetName"]  = ("Quêtes", "Quests"),
        ["WidgetDesc"]  = ("Complétion des quêtes du journal par catégorie, avec la liste des quêtes qu'il reste à faire et leur PNJ de départ.",
                           "Quest journal completion by category, with the list of quests left to do and their starting NPC."),
        ["Label"]       = ("Quêtes", "Quests"),
        ["Percent"]     = ("{0} %", "{0}%"),
        ["Count"]       = ("{0}/{1}", "{0}/{1}"),
        ["Loading"]     = ("Chargement…", "Loading…"),
        ["NotLoggedIn"] = ("Pas de personnage connecté", "No character logged in"),
        ["ReadError"]   = ("Erreur de lecture", "Read error"),

        // Réglages
        ["Cfg.Display"]               = ("Affichage dans la barre", "Toolbar display"),
        ["Cfg.Display.Desc"]          = ("Pourcentage de complétion, ou nombre de quêtes faites sur le total.", "Completion percentage, or number of quests done out of the total."),
        ["Cfg.Display.Percent"]       = ("Pourcentage (82 %)", "Percentage (82%)"),
        ["Cfg.Display.Count"]         = ("Nombre (1234/1500)", "Count (1234/1500)"),
        ["Cfg.ShowLabel"]             = ("Afficher le libellé", "Show the label"),
        ["Cfg.ShowLabel.Desc"]        = ("Affiche « Quêtes » (ou le nom de la catégorie suivie) devant la valeur.", "Shows \"Quests\" (or the tracked category's name) before the value."),
        ["Cfg.Track"]                 = ("Suivre", "Track"),
        ["Cfg.Track.Desc"]            = ("Ce que la barre affiche : toutes les quêtes, une section ou une seule catégorie du journal.", "What the toolbar shows: all quests, one journal section or a single category."),
        ["Cfg.Track.All"]             = ("Toutes les quêtes", "All quests"),
        ["Cfg.HideComplete"]          = ("Masquer les catégories à 100 %", "Hide categories at 100%"),
        ["Cfg.HideComplete.Desc"]     = ("Dans la liste, n'affiche que les catégories où il reste des quêtes.", "Only list categories that still have quests left."),
        ["Cfg.HideLocked"]            = ("Masquer les contenus pas encore débloqués", "Hide content not unlocked yet"),
        ["Cfg.HideLocked.Desc"]       = ("Ne compte pas les quêtes d'une extension que le personnage n'a pas encore atteinte dans le scénario principal.",
                                         "Leaves out quests from an expansion the character has not reached yet in the main scenario."),
        ["Cfg.OpenMap"]               = ("Ouvrir la carte", "Open the map"),
        ["Cfg.OpenMap.Desc"]          = ("Clic sur une quête : ouvre aussi la carte sur le drapeau.", "Clicking a quest also opens the map on the flag."),

        // Barre et infobulle
        ["Tooltip.Title"]   = ("Quêtes du journal : {0} ({1}/{2})", "Journal quests: {0} ({1}/{2})"),
        ["Tooltip.Tracked"] = ("Suivi : {0} — {1} ({2}/{3})", "Tracking: {0} — {1} ({2}/{3})"),
        ["Tooltip.Line"]    = ("{0} : {1} ({2}/{3})", "{0}: {1} ({2}/{3})"),
        ["Tooltip.Hint"]    = ("Clic : détail par catégorie", "Click: details by category"),

        // Popup
        ["Back"]            = ("← Retour", "← Back"),
        ["AllDone"]         = ("Toutes les quêtes de cette catégorie sont faites.", "Every quest in this category is done."),
        ["NothingLeft"]     = ("Toutes les catégories sont terminées.", "Every category is complete."),
        ["Level"]           = ("Nv {0}", "Lv {0}"),
        ["Row.Flag"]        = ("Clic : drapeau sur le PNJ de départ", "Click: flag on the starting NPC"),
        ["Row.NoLocation"]  = ("Emplacement du PNJ inconnu", "NPC location unknown"),
        ["Footer.Level1"]   = ("{0} catégories · clic : quêtes restantes", "{0} categories · click: quests left"),
        ["Footer.Level2"]   = ("{0} quêtes restantes · clic : drapeau sur le PNJ de départ", "{0} quests left · click: flag on the starting NPC"),
        ["Footer.Level2.One"] = ("1 quête restante · clic : drapeau sur le PNJ de départ", "1 quest left · click: flag on the starting NPC"),

        // Chat
        ["Chat.Flag"]       = ("{0} — {1}", "{0} — {1}"),
        ["Chat.Sep"]        = (" : ", ": "),
        ["Chat.NoLocation"] = ("Le jeu n'indique pas où commence cette quête.", "The game data does not say where this quest starts."),
    };

    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        return IsFrench ? entry.Fr : entry.En;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>Pourcentage arrondi vers le bas, pour ne jamais afficher 100 % s'il reste une quête.</summary>
    public static string Percent(int done, int total)
    {
        int pct = total == 0 ? 100 : done >= total ? 100 : (int)Math.Floor(done * 100.0 / total);
        return T("Percent", pct);
    }
}
