namespace Umbra.QuestLog;

/// <summary>
/// Popup du widget, sur deux niveaux.
///  1. Les catégories du journal, sous l'en-tête de leur section, avec une barre de progression et « faites / total ».
///  2. Clic sur une catégorie : les quêtes qui restent (nom, niveau, PNJ et zone de départ) et un bouton retour.
///     Clic sur une quête : drapeau sur la carte à l'emplacement du PNJ de départ.
/// </summary>
public sealed class QuestLogPopup : WidgetPopup
{
    private const int ListWidth  = 560;
    private const int ListHeight = 420;
    private const int BarWidth   = 140;

    private static readonly Color DoneColor = new(0xFF66DD66);

    private readonly QuestLogWidget _widget;

    protected override Node Node { get; }

    private Node Header => Node.QuerySelector(".ql-header")!;
    private Node List   => Node.QuerySelector(".ql-list")!;
    private Node Status => Node.QuerySelector(".ql-status")!;
    private Node Footer => Node.QuerySelector(".ql-footer")!;

    /// <summary>Catégorie ouverte (niveau 2), ou null pour la liste des catégories.</summary>
    private uint? _category;

    private string _signature = string.Empty;

    public QuestLogPopup(QuestLogWidget widget)
    {
        _widget = widget;

        Node = new() {
            Id         = "ql-popup",
            Stylesheet = PopupStylesheet,
            ChildNodes = [
                new() { ClassList = ["ql-header"] },
                new() { ClassList = ["ql-list"], Overflow = false },
                new() { ClassList = ["ql-status"] },
                new() { ClassList = ["ql-footer"] },
            ],
        };
    }

    protected override void OnOpen()
    {
        _category  = null;
        _signature = string.Empty;
    }

    protected override void OnUpdate()
    {
        var progress = _widget.Progress;
        bool hideComplete = _widget.GetConfigValue<bool>("HideComplete");

        string signature = $"{_widget.ProgressVersion}|{_category}|{hideComplete}|{Strings.IsFrench}";
        if (signature == _signature) return;
        _signature = signature;

        if (progress == null) {
            ShowMessage(QuestLogWidget.Catalog == null ? Strings.T("ReadError") : Strings.T("NotLoggedIn"));
            return;
        }

        if (_category is { } id && progress.Categories.TryGetValue(id, out var category)) {
            RenderQuests(category);
        } else {
            _category = null;
            RenderCategories(progress, hideComplete);
        }
    }

    private void Open(uint? categoryId)
    {
        _category  = categoryId;
        _signature = string.Empty;
    }

    // ------------------------------------------------------- Niveau 1 : catégories

    private void RenderCategories(QuestProgress progress, bool hideComplete)
    {
        Header.Clear();
        Header.AppendChild(new() { ClassList = ["ql-title"], NodeValue = Strings.T("WidgetName") });
        Header.AppendChild(new() {
            ClassList = ["ql-title-value"],
            NodeValue = $"{Strings.Percent(progress.Done, progress.Total)} · {progress.Done}/{progress.Total}",
        });

        List.Clear();
        int shown = 0;

        foreach (var section in progress.Sections) {
            var categories = section.Categories.Where(c => !hideComplete || c.Done < c.Total).ToList();
            if (categories.Count == 0) continue;

            List.AppendChild(new() {
                ClassList  = ["ql-section"],
                ChildNodes = [
                    new() { ClassList = ["ql-section-name"], NodeValue = section.Section.Name },
                    new() { ClassList = ["ql-section-value"], NodeValue = $"{Strings.Percent(section.Done, section.Total)} · {section.Done}/{section.Total}" },
                ],
            });

            foreach (var c in categories) {
                uint id   = c.Category.Id;
                bool full = c.Done >= c.Total;

                var row = new Node {
                    ClassList  = ["ql-row"],
                    ChildNodes = [
                        new() { ClassList = ["ql-cell", "cat-name"], NodeValue = c.Category.Name },
                        Bar(c.Ratio, full),
                        new() { ClassList = ["ql-cell", "count"], NodeValue = $"{c.Done}/{c.Total}" },
                    ],
                };

                if (full) row.QuerySelector(".count")!.Style.Color = DoneColor;
                row.OnClick += _ => Open(id);
                List.AppendChild(row);
                shown++;
            }
        }

        if (shown == 0) {
            ShowMessage(Strings.T("NothingLeft"), keepHeader: true);
            return;
        }

        Status.Style.IsVisible = false;
        List.Style.IsVisible   = true;
        Footer.NodeValue       = Strings.T("Footer.Level1", shown);
    }

    private static Node Bar(float ratio, bool full)
    {
        int width = (int)Math.Round(Math.Clamp(ratio, 0f, 1f) * BarWidth);

        return new() {
            ClassList  = ["ql-bar"],
            ChildNodes = [
                new() {
                    ClassList = full ? ["ql-bar-fill", "full"] : ["ql-bar-fill"],
                    Style     = new() { Size = new(width, 8), IsVisible = width > 0 },
                },
            ],
        };
    }

    // ---------------------------------------------------------- Niveau 2 : quêtes

    private void RenderQuests(CategoryProgress category)
    {
        Header.Clear();

        var back = new Node { ClassList = ["ql-back"], NodeValue = Strings.T("Back") };
        back.OnClick += _ => Open(null);
        Header.AppendChild(back);
        Header.AppendChild(new() { ClassList = ["ql-title"], NodeValue = category.Category.Name });
        Header.AppendChild(new() {
            ClassList = ["ql-title-value"],
            NodeValue = $"{Strings.Percent(category.Done, category.Total)} · {category.Done}/{category.Total}",
        });

        List.Clear();

        if (category.Remaining.Count == 0) {
            ShowMessage(Strings.T("AllDone"), keepHeader: true);
            return;
        }

        bool openMap = _widget.GetConfigValue<bool>("OpenMap");

        foreach (var q in category.Remaining) {
            var quest = q;
            string where = string.IsNullOrEmpty(q.IssuerName) ? q.ZoneName : $"{q.IssuerName} — {q.ZoneName}";

            var row = new Node {
                ClassList  = ["ql-row"],
                Tooltip    = q.HasLocation ? Strings.T("Row.Flag") : Strings.T("Row.NoLocation"),
                ChildNodes = [
                    new() { ClassList = ["ql-cell", "quest-name"], NodeValue = q.Name },
                    new() { ClassList = ["ql-cell", "level"], NodeValue = Strings.T("Level", q.Level) },
                    new() { ClassList = ["ql-cell", "where"], NodeValue = where },
                ],
            };

            row.OnClick += _ => OnQuestClicked(quest, openMap);
            List.AppendChild(row);
        }

        Status.Style.IsVisible = false;
        List.Style.IsVisible   = true;
        Footer.NodeValue       = category.Remaining.Count == 1
            ? Strings.T("Footer.Level2.One")
            : Strings.T("Footer.Level2", category.Remaining.Count);
    }

    private void OnQuestClicked(QuestEntry quest, bool openMap)
    {
        try {
            QuestFlag.Place(quest, openMap);
            if (openMap && quest.HasLocation) Close();
        } catch (Exception e) {
            Logger.Error($"[QuestLog] Clic sur la quête {quest.Id} : {e}");
        }
    }

    private void ShowMessage(string message, bool keepHeader = false)
    {
        if (!keepHeader) Header.Clear();
        List.Clear();
        List.Style.IsVisible   = false;
        Status.Style.IsVisible = true;
        Status.NodeValue       = message;
        Footer.NodeValue       = string.Empty;
    }

    // ------------------------------------------------------------ Stylesheet

    private static Style Text(int width, Anchor align, int fontSize = 13) => new() {
        Size         = new(width, 22),
        FontSize     = fontSize,
        TextAlign    = align,
        Anchor       = Anchor.MiddleLeft,
        Color        = new("Widget.PopupMenuText"),
        OutlineColor = new("Widget.PopupMenuTextOutline"),
        OutlineSize  = 1,
        WordWrap     = false,
        TextOverflow = false,
    };

    private static Style Muted(int width, Anchor align, int fontSize = 12) => new() {
        Size         = new(width, 22),
        FontSize     = fontSize,
        TextAlign    = align,
        Anchor       = Anchor.MiddleLeft,
        Color        = new("Widget.PopupMenuTextMuted"),
        OutlineColor = new("Widget.PopupMenuTextOutline"),
        OutlineSize  = 1,
        WordWrap     = false,
        TextOverflow = false,
    };

    private static Stylesheet PopupStylesheet { get; } = new([
        new("#ql-popup", new() { Flow = Flow.Vertical, Padding = new(8), Gap = 6 }),

        new(".ql-header", new() {
            Flow        = Flow.Horizontal,
            Gap         = 8,
            Size        = new(ListWidth, 0),
            Padding     = new() { Bottom = 4 },
            BorderColor = new() { Bottom = new("Widget.Border") },
            BorderWidth = new() { Bottom = 1 },
        }),
        new(".ql-title", Text(0, Anchor.MiddleLeft, 14)),
        new(".ql-title-value", Muted(0, Anchor.MiddleLeft)),
        // Même ancrage que le titre : Una.Drawing ne fait se suivre que les nœuds de même ancre.
        new(".ql-back", new() {
            Anchor       = Anchor.MiddleLeft,
            Padding      = new(2, 8),
            BorderRadius = 4,
            FontSize     = 12,
            Size         = new(0, 22),
            TextAlign    = Anchor.MiddleCenter,
            StrokeColor  = new("Widget.Border"),
            StrokeWidth  = 1,
            Color        = new("Widget.PopupMenuText"),
            OutlineColor = new("Widget.PopupMenuTextOutline"),
            OutlineSize  = 1,
        }),
        new(".ql-back:hover", new() { BackgroundColor = new("Widget.PopupMenuBackgroundHover") }),

        new(".ql-list", new() {
            Flow                = Flow.Vertical,
            Size                = new(ListWidth, ListHeight),
            ScrollbarTrackColor = new(0x30000000),
            ScrollbarThumbColor = new("Widget.Border"),
        }),

        new(".ql-section", new() {
            Flow        = Flow.Horizontal,
            Gap         = 8,
            Size        = new(ListWidth - 16, 0),
            Padding     = new() { Top = 6, Bottom = 2 },
            BorderColor = new() { Bottom = new("Widget.Border") },
            BorderWidth = new() { Bottom = 1 },
        }),
        new(".ql-section-name", Muted(0, Anchor.MiddleLeft, 12)),
        new(".ql-section-value", Muted(0, Anchor.MiddleLeft, 11)),

        new(".ql-row", new() { Flow = Flow.Horizontal, Gap = 8, Padding = new(1, 4), BorderRadius = 3 }),
        new(".ql-row:hover", new() { BackgroundColor = new("Widget.PopupMenuBackgroundHover") }),

        new(".ql-cell.cat-name", Text(ListWidth - BarWidth - 110, Anchor.MiddleLeft)),
        new(".ql-cell.count", Text(70, Anchor.MiddleRight)),
        new(".ql-cell.quest-name", Text(250, Anchor.MiddleLeft)),
        new(".ql-cell.level", Muted(50, Anchor.MiddleRight)),
        new(".ql-cell.where", Muted(ListWidth - 340, Anchor.MiddleLeft)),

        new(".ql-bar", new() {
            Size            = new(BarWidth, 8),
            Anchor          = Anchor.MiddleLeft,
            Margin          = new() { Top = 7 },
            BorderRadius    = 3,
            BackgroundColor = new(0x40000000),
            StrokeColor     = new("Widget.Border"),
            StrokeWidth     = 1,
        }),
        new(".ql-bar-fill", new() { BorderRadius = 3, BackgroundColor = new("Widget.ProgressBar") }),
        new(".ql-bar-fill.full", new() { BackgroundColor = new(0xFF66DD66) }),

        new(".ql-status", new() {
            Size         = new(ListWidth, 60),
            FontSize     = 13,
            TextAlign    = Anchor.MiddleCenter,
            WordWrap     = true,
            IsVisible    = false,
            Color        = new("Widget.PopupMenuTextMuted"),
            OutlineColor = new("Widget.PopupMenuTextOutline"),
            OutlineSize  = 1,
        }),
        new(".ql-footer", new() {
            Size         = new(ListWidth, 0),
            FontSize     = 11,
            WordWrap     = true,
            Color        = new("Widget.PopupMenuTextMuted"),
            OutlineColor = new("Widget.PopupMenuTextOutline"),
            OutlineSize  = 1,
        }),
    ]);
}
