using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Marines.ControlComputer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Client._RMC14.Marines.ControlComputer;

[UsedImplicitly]
public sealed class MedalsPanelBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MedalsPanelWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<MedalsPanelWindow>();
        _window.GrantNewMedalButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerMedalMsg());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        
        if (_window == null || state is not MarineMedalsPanelBuiState medalsState)
            return;

        UpdateRecommendations(medalsState);
    }

    private void UpdateRecommendations(MarineMedalsPanelBuiState state)
    {
        if (_window == null)
            return;

        _window.RecommendationsList.DisposeAllChildren();

        foreach (var group in state.RecommendationGroups)
        {
            // Create panel container for each marine group
            var groupPanel = new PanelContainer
            {
                HorizontalExpand = true
            };
            
            var panelStyle = new StyleBoxFlat
            {
                BorderColor = new Color(58, 58, 58), // #3A3A3A
                BorderThickness = new Robust.Shared.Maths.Thickness(1),
                BackgroundColor = new Color(31, 31, 31) // #1F1F1F
            };
            groupPanel.PanelOverride = panelStyle;

            var groupContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Robust.Shared.Maths.Thickness(8),
                HorizontalExpand = true
            };

            // Header: Rank, Squad (if exists), Job, Name
            var headerParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(group.Rank))
                headerParts.Add(group.Rank);
            if (!string.IsNullOrWhiteSpace(group.Squad))
                headerParts.Add($"({group.Squad})");
            if (!string.IsNullOrWhiteSpace(group.Job))
                headerParts.Add(group.Job);
            headerParts.Add(group.Name);

            var headerText = string.Join(" ", headerParts);
            var headerLabel = new RichTextLabel
            {
                HorizontalExpand = true
            };
            headerLabel.SetMessage(FormattedMessage.FromMarkupOrThrow($"[bold]{headerText}[/bold]"));
            groupContainer.AddChild(headerLabel);

            // Add blue separator after header
            var separator = new BlueHorizontalSeparator();
            groupContainer.AddChild(separator);

            // Recommendations
            var recommendationsContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Robust.Shared.Maths.Thickness(4, 0),
                SeparationOverride = 3,
                HorizontalExpand = true
            };

            foreach (var recommendation in group.Recommendations)
            {
                var recContainer = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalExpand = true
                };

                var recommenderText = $"Recommender: {recommendation.RecommenderName} ()";
                var recommenderLabel = new RichTextLabel
                {
                    HorizontalExpand = true
                };
                recommenderLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(recommenderText));
                recContainer.AddChild(recommenderLabel);

                var reasonText = $"Reason: {recommendation.Reason}";
                var reasonLabel = new RichTextLabel
                {
                    HorizontalExpand = true
                };
                reasonLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(reasonText));
                recContainer.AddChild(reasonLabel);

                recommendationsContainer.AddChild(recContainer);
            }

            groupContainer.AddChild(recommendationsContainer);

            groupPanel.AddChild(groupContainer);
            _window.RecommendationsList.AddChild(groupPanel);
        }
    }
}

