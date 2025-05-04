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
        _window.CurrentPointsLabel.Text = $"{tree.Points.Double():F1}";
        _window.CurrentTierLabel.Text = $"{tree.Tier}";
        _window.TotalPointsLabel.Text = $"Total earned credits: {tree.TotalEarned.Double():F1}";
        _window.DocumentsLabel.Text = $"{tree.Documents.Current} / {tree.Documents.Total}";
        // _window.UploadDataLabel.Text = $"{tree.UploadData.Current} / {tree.UploadData.Total}";
        _window.RetrieveItemsLabel.Text = $"{tree.RetrieveItems.Current} / {tree.RetrieveItems.Total}";
        // _window.MiscellaneousLabel.Text = $"{tree.Miscellaneous.Current} / {tree.Miscellaneous.Total}";
        // _window.AnalyzeChemicalsLabel.Text = $"{tree.AnalyzeChemicals} / \u221e";
        _window.RescueSurvivorsLabel.Text = $"{tree.RescueSurvivors} / \u221e";
        _window.RecoverCorpsesLabel.Text = $"{tree.RecoverCorpses} / \u221e";
        _window.ColonyCommunicationsLabel.Text = tree.ColonyCommunications ? "Online" : "Offline";
        _window.ColonyPowerLabel.Text = tree.ColonyPower ? "Online" : "Offline";

        _window.CluesContainer.DisposeAllChildren();
        foreach (var (category, clues) in comp.Tree.Clues)
        {
            var scroll = new ScrollContainer
            {
                HScrollEnabled = false,
                VScrollEnabled = true,
            };

            var container = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
            scroll.AddChild(container);
            foreach (var (_, clue) in clues)
            {
                container.AddChild(new Label
                {
                    Text = clue,
                    Margin = new Thickness(2),
                });
            }

            _window.CluesContainer.AddChild(scroll);
            TabContainer.SetTabTitle(scroll, Loc.GetString(category));
            TabContainer.SetTabVisible(scroll, true);
        }
    }
}
