using FFXIVClientStructs.FFXIV.Client.Game;

namespace Umbra.QuestLog;

/// <summary>
/// Le nom et la description d'un [ToolbarWidget] sont des constantes. Umbra lit l'attribut
/// avec GetCustomAttribute au démarrage de son framework, ce qui accepte une sous-classe :
/// celle-ci choisit le nom et la description dans la langue d'Umbra à ce moment-là.
/// </summary>
internal sealed class LocalizedToolbarWidgetAttribute() : ToolbarWidgetAttribute(
    "QuestLog",
    Strings.T("WidgetName"),
    Strings.T("WidgetDesc"),
    ["quest", "quests", "quête", "quêtes", "journal", "completion", "complétion", "sidequest", "annexe", "scenario", "scénario", "tracker"]
);

/// <summary>
/// Complétion des quêtes du journal. Dans la barre : icône et pourcentage (ou faites/total),
/// global, d'une section ou d'une catégorie. Au clic : les catégories, puis les quêtes qui restent.
/// Le calcul est refait seulement quand une quête est terminée (QuestSignal), à la connexion,
/// ou quand un réglage qui change les totaux est modifié.
/// </summary>
[LocalizedToolbarWidget]
public sealed class QuestLogWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.Icon |
        StandardWidgetFeatures.CustomizableIcon |
        StandardWidgetFeatures.ProgressBar;

    protected override string          DefaultIconType        => IconTypeFontAwesome;
    protected override FontAwesomeIcon DefaultFontAwesomeIcon => FontAwesomeIcon.BookOpen;

    private QuestLogPopup? _popup;

    public override WidgetPopup? Popup => _popup ??= new QuestLogPopup(this);

    /// <summary>Dernier calcul, ou null tant qu'aucun personnage n'est connecté.</summary>
    public QuestProgress? Progress { get; private set; }

    /// <summary>Augmente à chaque nouveau calcul (la popup s'en sert pour se redessiner).</summary>
    public int ProgressVersion { get; private set; }

    private string   _signature   = string.Empty;
    private DateTime _nextCheck   = DateTime.MinValue;
    private DateTime _nextPolling = DateTime.MinValue;
    private int      _polling;

    // ------------------------------------------------------------------ Données

    private static QuestCatalog? _catalog;
    private static string?       _catalogError;

    /// <summary>Catalogue des quêtes, construit une fois (feuilles du jeu dans la langue du client).</summary>
    public static QuestCatalog? Catalog
    {
        get {
            if (_catalog != null || _catalogError != null) return _catalog;

            try {
                _catalog = QuestCatalog.Build(Framework.Service<IDataManager>().Excel);
            } catch (Exception e) {
                _catalogError = e.Message;
                Logger.Error($"[QuestLog] Lecture des quêtes impossible : {e}");
            }

            return _catalog;
        }
    }

    // ------------------------------------------------------------------ Config

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        string cat = Strings.T("WidgetName");

        return [
            ..base.GetConfigVariables(),
            new SelectWidgetConfigVariable(
                "Display", Strings.T("Cfg.Display"), Strings.T("Cfg.Display.Desc"), "Percent",
                new() { ["Percent"] = Strings.T("Cfg.Display.Percent"), ["Count"] = Strings.T("Cfg.Display.Count") }
            ) { Category = cat },
            new BooleanWidgetConfigVariable("ShowLabel", Strings.T("Cfg.ShowLabel"), Strings.T("Cfg.ShowLabel.Desc"), true) { Category = cat },
            new SelectWidgetConfigVariable("Track", Strings.T("Cfg.Track"), Strings.T("Cfg.Track.Desc"), "All", TrackOptions()) { Category = cat },
            new BooleanWidgetConfigVariable("HideComplete", Strings.T("Cfg.HideComplete"), Strings.T("Cfg.HideComplete.Desc"), false) { Category = cat },
            new BooleanWidgetConfigVariable("HideLocked", Strings.T("Cfg.HideLocked"), Strings.T("Cfg.HideLocked.Desc"), false) { Category = cat },
            new BooleanWidgetConfigVariable("OpenMap", Strings.T("Cfg.OpenMap"), Strings.T("Cfg.OpenMap.Desc"), true) { Category = cat },
        ];
    }

    /// <summary>Valeurs internes : « All », « S{id} » (section), « C{id} » (catégorie). Libellés dans la langue du client.</summary>
    private static Dictionary<string, string> TrackOptions()
    {
        Dictionary<string, string> options = new() { ["All"] = Strings.T("Cfg.Track.All") };

        var catalog = Catalog;
        if (catalog == null) return options;

        foreach (var section in catalog.Sections) {
            options[$"S{section.Id}"] = section.Name;
            foreach (var category in section.Categories) options[$"C{category.Id}"] = $"{section.Name} › {category.Name}";
        }

        return options;
    }

    // --------------------------------------------------------------- Lifecycle

    protected override void OnLoad()
    {
        QuestSignal.Acquire();

        SetText(Strings.T("Label"));
        SetTooltip(Strings.T("WidgetName"));
    }

    protected override void OnUnload()
    {
        QuestSignal.Release();
    }

    protected override void OnDraw()
    {
        if (DateTime.UtcNow < _nextCheck) return;
        _nextCheck = DateTime.UtcNow.AddMilliseconds(500);

        if (!Framework.Service<IClientState>().IsLoggedIn) {
            if (Progress != null) {
                Progress = null;
                ProgressVersion++;
            }
            _signature = string.Empty;
            ShowStatus(Strings.T("NotLoggedIn"));
            return;
        }

        var catalog = Catalog;
        if (catalog == null) {
            ShowStatus(Strings.T("ReadError"));
            return;
        }

        // Sans IUnlockState, on se rabat sur un recalcul toutes les 15 secondes.
        if (!QuestSignal.IsAvailable && DateTime.UtcNow >= _nextPolling) {
            _nextPolling = DateTime.UtcNow.AddSeconds(15);
            _polling++;
        }

        bool hideLocked = GetConfigValue<bool>("HideLocked");
        uint gc         = Framework.Service<IPlayer>().GrandCompanyId;

        string signature = $"{QuestSignal.Version}|{_polling}|{hideLocked}|{gc}";
        if (signature != _signature) {
            try {
                Progress = Compute(catalog, gc, hideLocked);
            } catch (Exception e) {
                Logger.Error($"[QuestLog] Calcul impossible : {e}");
                ShowStatus(Strings.T("ReadError"));
                return;
            }

            _signature = signature;
            ProgressVersion++;
        }

        UpdateToolbar();
    }

    private static QuestProgress Compute(QuestCatalog catalog, uint grandCompany, bool hideLocked)
        => QuestProgress.Evaluate(catalog, new PlayerContext(id => QuestManager.IsQuestComplete(id), grandCompany, hideLocked));

    // ----------------------------------------------------------------- Display

    private void ShowStatus(string tooltip)
    {
        SetText(GetConfigValue<bool>("ShowLabel") ? Strings.T("Label") : "—");
        SetProgressBarConstraint(0, 1);
        SetProgressBarValue(0);
        SetTooltip($"{Strings.T("WidgetName")}\n{tooltip}");
    }

    /// <summary>Ce que la barre suit : (libellé, faites, total).</summary>
    private (string Label, int Done, int Total) Tracked(QuestProgress p)
    {
        string track = GetConfigValue<string>("Track");

        if (track.Length > 1 && uint.TryParse(track[1..], out uint id)) {
            if (track[0] == 'S' && p.Sections.FirstOrDefault(s => s.Section.Id == id) is { } s) return (s.Section.Name, s.Done, s.Total);
            if (track[0] == 'C' && p.Categories.TryGetValue(id, out var c)) return (c.Category.Name, c.Done, c.Total);
        }

        return (Strings.T("Label"), p.Done, p.Total);
    }

    private void UpdateToolbar()
    {
        var p = Progress;
        if (p == null) return;

        var (label, done, total) = Tracked(p);

        string value = GetConfigValue<string>("Display") == "Count" ? Strings.T("Count", done, total) : Strings.Percent(done, total);
        SetText(GetConfigValue<bool>("ShowLabel") ? $"{label} {value}" : value);

        SetProgressBarConstraint(0, Math.Max(1, total));
        SetProgressBarValue(done);

        List<string> lines = [Strings.T("Tooltip.Title", Strings.Percent(p.Done, p.Total), p.Done, p.Total)];

        if (GetConfigValue<string>("Track") != "All" && label != Strings.T("Label")) {
            lines.Add(Strings.T("Tooltip.Tracked", label, Strings.Percent(done, total), done, total));
        }

        lines.Add("");
        foreach (var s in p.Sections.Take(5)) {
            lines.Add(Strings.T("Tooltip.Line", s.Section.Name, Strings.Percent(s.Done, s.Total), s.Done, s.Total));
        }

        lines.Add("");
        lines.Add(Strings.T("Tooltip.Hint"));

        SetTooltip(string.Join("\n", lines));
    }
}
