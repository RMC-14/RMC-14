using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client._RMC14.Marines.ControlComputer;

[UsedImplicitly]
public sealed class MedalsPanelBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [ViewVariables]
    private MedalsPanelWindow? _window;

    // Dictionary to store LastPlayerId -> PanelContainer mapping for quick removal
    private readonly Dictionary<string, PanelContainer> _recommendationGroups = new();

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<MedalsPanelWindow>();
        _window.GrantNewMedalButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerMedalMsg());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is MarineControlComputerRemoveRecommendationGroupMsg removeMsg)
        {
            RemoveRecommendationGroup(removeMsg.LastPlayerId);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not MarineMedalsPanelBuiState medalsState)
            return;

        UpdateRecommendations(medalsState);
    }

    private static StyleBoxFlat CreateStyleBox(Color backgroundColor) => new()
    {
        BackgroundColor = backgroundColor,
        BorderThickness = new Robust.Shared.Maths.Thickness(0),
        Padding = new Robust.Shared.Maths.Thickness(2)
    };

    private BoxContainer CreateRecommendationContainer(MarineRecommendationInfo recommendation)
    {
        var recContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        // Recommender: Rank and Name
        var recommenderLabelText = Loc.GetString("rmc-medal-panel-recommender-label");
        var recommenderText = $"{recommenderLabelText} {string.Join(" ", recommendation.RecommenderRank, recommendation.RecommenderName)}";
        var recommenderLabel = new RichTextLabel
        {
            HorizontalExpand = true
        };
        recommenderLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(recommenderText));
        recContainer.AddChild(recommenderLabel);

        // Job: Squad (if exists) and Job
        var jobLabelText = Loc.GetString("rmc-medal-panel-job-label");
        var squadPart = string.IsNullOrEmpty(recommendation.RecommenderSquad) ? null : $"({recommendation.RecommenderSquad})";
        var jobText = $"{jobLabelText} {string.Join(" ", squadPart, recommendation.RecommenderJob)}";
        var jobLabel = new RichTextLabel
        {
            HorizontalExpand = true
        };
        jobLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(jobText));
        recContainer.AddChild(jobLabel);

        // Reason with copy button
        var reasonContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };

        var reasonLabelText = Loc.GetString("rmc-medal-panel-reason-label");
        var reasonText = $"{reasonLabelText} {recommendation.Reason}";
        var reasonLabel = new RichTextLabel
        {
            HorizontalExpand = true,
            Margin = new Robust.Shared.Maths.Thickness(0, 0, 6, 0),
        };
        reasonLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(reasonText));
        reasonContainer.AddChild(reasonLabel);

        // Copy button with icon
        var copyButton = new ContainerButton
        {
            SetWidth = 28,
            SetHeight = 28,
            HorizontalAlignment = Control.HAlignment.Right
        };

        // Background colors relative to container background (#1F1F1F)
        var normalColor = new Color(31, 31, 31); // Same as container background
        var hoverColor = new Color(47, 47, 47); // Lighter than container background
        var pressedColor = new Color(23, 55, 23); // Green like standard button pressed state

        var normalStyle = CreateStyleBox(normalColor);
        copyButton.StyleBoxOverride = normalStyle;

        // Track hover state
        var isHovered = false;

        // Handle hover - change color when mouse enters
        copyButton.OnMouseEntered += _ =>
        {
            isHovered = true;
            if (!copyButton.Pressed)
                copyButton.StyleBoxOverride = CreateStyleBox(hoverColor);
        };

        // Handle hover exit - restore normal color
        copyButton.OnMouseExited += _ =>
        {
            isHovered = false;
            if (!copyButton.Pressed)
                copyButton.StyleBoxOverride = normalStyle;
        };

        // Handle button press - change to green color while button is held
        copyButton.OnButtonDown += _ =>
        {
            copyButton.StyleBoxOverride = CreateStyleBox(pressedColor);
        };

        // Handle button release - restore color based on hover state
        copyButton.OnButtonUp += _ =>
        {
            copyButton.StyleBoxOverride = isHovered ? CreateStyleBox(hoverColor) : normalStyle;
        };

        var copyIcon = new TextureRect
        {
            SetWidth = 20,
            SetHeight = 20,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center
        };

        // Load copy and check icon textures (PNG versions created from SVG)
        var copyTexture = _resourceCache.GetTexture(new ResPath("/Textures/_RMC14/Interface/VerbIcons/copy.png"));
        var checkTexture = _resourceCache.GetTexture(new ResPath("/Textures/_RMC14/Interface/VerbIcons/check.png"));

        copyIcon.Texture = copyTexture;
        copyButton.AddChild(copyIcon);

        // Handle copy button press
        copyButton.OnPressed += _ =>
        {
            _clipboard.SetText(recommendation.Reason);

            // Replace icon with check mark
            copyIcon.Texture = checkTexture;

            // Schedule icon restoration after 1 second
            Timer.Spawn(System.TimeSpan.FromSeconds(1), () =>
            {
                copyIcon.Texture = copyTexture;
            });
        };

        reasonContainer.AddChild(copyButton);
        recContainer.AddChild(reasonContainer);

        return recContainer;
    }

    private void UpdateRecommendations(MarineMedalsPanelBuiState state)
    {
        if (_window == null)
            return;

        _window.RecommendationsList.DisposeAllChildren();
        _recommendationGroups.Clear();

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
            var squadPart = string.IsNullOrEmpty(group.Squad) ? null : $"({group.Squad})";
            var headerText = string.Join(" ", group.Rank, squadPart, group.Job, group.Name);
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
                SeparationOverride = 12,
                HorizontalExpand = true
            };

            // Store references to hidden recommendation containers (3+)
            var hiddenRecommendationContainers = new List<BoxContainer>();

            // Add all recommendations
            for (int i = 0; i < group.Recommendations.Count; i++)
            {
                var recommendation = group.Recommendations[i];
                var recContainer = CreateRecommendationContainer(recommendation);

                // Hide 3rd and later recommendations by default
                if (i >= 2)
                {
                    recContainer.Visible = false;
                    hiddenRecommendationContainers.Add(recContainer);
                }

                recommendationsContainer.AddChild(recContainer);
            }

            groupContainer.AddChild(recommendationsContainer);

            // Add expand/collapse button separately below recommendations if there are hidden ones
            if (hiddenRecommendationContainers.Count > 0)
            {
                var isExpanded = false;
                var expandButton = new Button
                {
                    Text = "",
                    HorizontalExpand = true,
                    Margin = new Robust.Shared.Maths.Thickness(0, 8, 0, 0)
                };
                expandButton.AddStyleClass("OpenBoth");

                // Use RichTextLabel for bold text, matching the style of GrantNewMedalButton
                var arrowLabel = new RichTextLabel
                {
                    HorizontalAlignment = Control.HAlignment.Center,
                    VerticalAlignment = Control.VAlignment.Center,
                    HorizontalExpand = true
                };
                arrowLabel.SetMessage(FormattedMessage.FromMarkupOrThrow("[bold]v[/bold]"));
                expandButton.AddChild(arrowLabel);

                // Handle expand/collapse toggle
                expandButton.OnPressed += _ =>
                {
                    isExpanded = !isExpanded;
                    foreach (var container in hiddenRecommendationContainers)
                    {
                        container.Visible = isExpanded;
                    }
                    arrowLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(isExpanded ? "[bold]^[/bold]" : "[bold]v[/bold]"));
                };

                groupContainer.AddChild(expandButton);
            }

            // Approve and Reject buttons - right bottom corner
            var buttonsContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalAlignment = Control.HAlignment.Right,
                Margin = new Robust.Shared.Maths.Thickness(0, 8, 0, 0),
                SeparationOverride = 8
            };

            var approveButton = new Button
            {
                Text = Loc.GetString("rmc-medal-panel-approve-recommendation")
            };
            approveButton.AddStyleClass("OpenBoth");
            approveButton.AddStyleClass(StyleNano.StyleClassButtonColorGreen);
            approveButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerApproveRecommendationMsg { LastPlayerId = group.LastPlayerId });
            buttonsContainer.AddChild(approveButton);

            var rejectButton = new Button
            {
                Text = Loc.GetString("rmc-medal-panel-reject-recommendation")
            };
            rejectButton.AddStyleClass("OpenBoth");
            rejectButton.AddStyleClass(StyleNano.StyleClassButtonColorRed);
            rejectButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerRejectRecommendationMsg { LastPlayerId = group.LastPlayerId });
            buttonsContainer.AddChild(rejectButton);
            groupContainer.AddChild(buttonsContainer);

            groupPanel.AddChild(groupContainer);
            _window.RecommendationsList.AddChild(groupPanel);

            // Store reference for quick removal
            _recommendationGroups[group.LastPlayerId] = groupPanel;
        }
    }

    private void RemoveRecommendationGroup(string lastPlayerId)
    {
        if (_window == null)
            return;

        if (_recommendationGroups.TryGetValue(lastPlayerId, out var panel))
        {
            panel.Orphan();
            _recommendationGroups.Remove(lastPlayerId);
        }
    }
}

