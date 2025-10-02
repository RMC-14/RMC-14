using Content.Shared._RMC14.Intel;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Intel;

[UsedImplicitly]
public sealed class ViewIntelObjectivesBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ViewIntelObjectivesWindow? _window;

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
        // _window.UploadDataLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.UploadData.Current), ("total", tree.UploadData.Total));
        _window.RetrieveItemsLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.RetrieveItems.Current), ("total", tree.RetrieveItems.Total));
        // _window.MiscellaneousLabel.Text = Loc.GetString("rmc-ui-intel-progress", ("current", tree.Miscellaneous.Current), ("total", tree.Miscellaneous.Total));
        // _window.AnalyzeChemicalsLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.AnalyzeChemicals));
        _window.RescueSurvivorsLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.RescueSurvivors));
        _window.RecoverCorpsesLabel.Text = Loc.GetString("rmc-ui-intel-infinite-progress", ("current", tree.RecoverCorpses));
        _window.ColonyCommunicationsLabel.Text = Loc.GetString("rmc-ui-intel-colony-status", ("online", tree.ColonyCommunications));
        _window.ColonyPowerLabel.Text = Loc.GetString("rmc-ui-intel-colony-status", ("online", tree.ColonyPower));

        _window.CluesContainer.DisposeAllChildren();
        foreach (var (category, clues) in comp.Tree.Clues)
        {
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

            foreach (var (_, clue) in clues)
            {
                container.AddChild(new Label
                {
                    Text = clue,
                    Margin = new Thickness(2, 1, 2, 1),
                    StyleClasses = { "Label" }
                });
            }

            scroll.AddChild(container);
            _window.CluesContainer.AddChild(scroll);
            TabContainer.SetTabTitle(scroll, Loc.GetString(category));
        }
    }
}
