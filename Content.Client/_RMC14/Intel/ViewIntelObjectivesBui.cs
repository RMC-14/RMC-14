using System.Linq;
using Content.Shared._RMC14.Intel;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Intel;

[UsedImplicitly]
public sealed class ViewIntelObjectivesBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ViewIntelObjectivesWindow? _window;

    private readonly List<(string PlainClue, Control Row, Control? AreaLabel)> _allClueRows = new();
    private bool _hideAreas;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ViewIntelObjectivesWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out ViewIntelObjectivesComponent? comp))
            return;

        var tree = comp.Tree;
        _window.CurrentPointsLabel.Text = Loc.GetString("rmc-ui-intel-points-value", ("value", tree.Points.Double().ToString("F1")));
        _window.CurrentTierLabel.Text = Loc.GetString("rmc-ui-intel-tier-value", ("value", tree.Tier));
        _window.TotalPointsLabel.Text = Loc.GetString("rmc-ui-intel-total-credits", ("value", tree.TotalEarned.Double().ToString("F1")));
        _window.DocumentsLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.Documents.Current), ("total", tree.Documents.Total));
        _window.UploadDataLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.UploadData.Current), ("total", tree.UploadData.Total));
        _window.RetrieveItemsLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.RetrieveItems.Current), ("total", tree.RetrieveItems.Total));
        _window.MiscellaneousLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.Miscellaneous.Current), ("total", tree.Miscellaneous.Total));
        // _window.AnalyzeChemicalsLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.AnalyzeChemicals));
        _window.RescueSurvivorsLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.RescueSurvivors));
        _window.RecoverCorpsesLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.RecoverCorpses));
        _window.ColonyCommunicationsLabel.Text = Loc.GetString("rmc-ui-intel-colony-status", ("online", tree.ColonyCommunications));
        _window.ColonyPowerLabel.Text = Loc.GetString("rmc-ui-intel-colony-status", ("online", tree.ColonyPower));

        _allClueRows.Clear();
        if (_window.CluesSearchBar != null)
            _window.CluesSearchBar.Text = string.Empty;

        _window.CluesContainer.DisposeAllChildren();
        if (comp.PersonalClues.Count > 0)
            AddClueTab("rmc-intel-personal", comp.PersonalClues.Values);

        foreach (var (category, clues) in comp.Tree.Clues)
            AddClueTab(category, clues.Values);

        if (_window.CluesSearchBar != null)
        {
            _window.CluesSearchBar.OnTextChanged -= OnSearchChanged;
            _window.CluesSearchBar.OnTextChanged += OnSearchChanged;
        }

        if (_window.HideAreasButton != null)
        {
            _window.HideAreasButton.OnPressed -= OnHideAreasPressed;
            _window.HideAreasButton.OnPressed += OnHideAreasPressed;
            UpdateHideAreasButton();
        }

        ApplyAreaVisibility();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        ApplyFilter(args.Text.Trim());
    }

    private void OnHideAreasPressed(BaseButton.ButtonEventArgs _)
    {
        _hideAreas = !_hideAreas;
        UpdateHideAreasButton();
        ApplyAreaVisibility();
    }

    private void UpdateHideAreasButton()
    {
        if (_window?.HideAreasButton == null)
            return;

        _window.HideAreasButton.Text = _hideAreas ? "Show Areas" : "Hide Areas";
    }

    private void ApplyFilter(string query)
    {
        foreach ((string plainClue, Control row, Control? _) in _allClueRows)
        {
            var visible = string.IsNullOrEmpty(query) ||
                          plainClue.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            row.Visible = visible;
        }
    }

    private void ApplyAreaVisibility()
    {
        foreach ((string _, Control _, Control? areaLabel) in _allClueRows)
        {
            if (areaLabel != null)
                areaLabel.Visible = !_hideAreas;
        }
    }

    private static string StripMarkup(string text)
    {
        var result = new System.Text.StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '[')
            {
                var end = text.IndexOf(']', i);
                if (end >= 0)
                {
                    i = end + 1;
                    continue;
                }
            }
            result.Append(text[i]);
            i++;
        }
        return result.ToString();
    }

    private void AddClueTab(string category, IEnumerable<string> clues)
    {
        if (_window is not { IsOpen: true })
            return;

        var scroll = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true,
        };

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(4),
        };

        var sortedClues = clues.OrderBy(c => StripMarkup(c).ToLowerInvariant());

        foreach (var clue in sortedClues)
        {
            var (row, areaLabel) = BuildClueRow(clue);
            container.AddChild(row);
            _allClueRows.Add((StripMarkup(clue), row, areaLabel));
        }

        scroll.AddChild(container);
        _window.CluesContainer.AddChild(scroll);
        TabContainer.SetTabTitle(scroll, Loc.GetString(category));
    }

    private static (Control Row, Control? AreaLabel) BuildClueRow(string clue)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(2, 1, 2, 1),
            HorizontalExpand = true,
        };

        const string separator = " in ";
        var splitIndex = clue.LastIndexOf(separator, StringComparison.OrdinalIgnoreCase);

        if (splitIndex < 0)
        {
            row.AddChild(new RichTextLabel
            {
                Text = clue,
                StyleClasses = { "Label" },
            });

            return (row, null);
        }

        var itemPart = clue[..splitIndex].TrimEnd('.');
        var areaPart = clue[(splitIndex + separator.Length)..].TrimEnd('.');

        row.AddChild(new RichTextLabel
        {
            Text = itemPart,
            StyleClasses = { "Label" },
        });

        row.AddChild(new Control
        {
            HorizontalExpand = true,
        });

        var areaLabel = new RichTextLabel
        {
            Text = areaPart,
            StyleClasses = { "Label" },
        };

        row.AddChild(areaLabel);

        return (row, areaLabel);
    }
}
