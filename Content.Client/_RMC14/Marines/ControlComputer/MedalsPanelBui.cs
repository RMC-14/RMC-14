using System.Linq;
using Content.Client._RMC14.Commendations;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Client.UserInterface.CustomControls;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client._RMC14.Marines.ControlComputer;

[UsedImplicitly]
public sealed class MedalsPanelBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    [ViewVariables]
    private MedalsPanelWindow? _window;

    // Dictionary to store LastPlayerId -> PanelContainer mapping for quick removal
    private readonly Dictionary<string, PanelContainer> _recommendationGroups = new();

    private sealed record MedalInfo(SpriteSpecifier Icon, string Name, string Description);
    private readonly Dictionary<string, MedalInfo> _medalsInfo = new();
    private bool _medalsInfoBuilt = false;

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
        else if (message is MarineControlComputerAddMedalMsg addMsg)
        {
            AddMedal(addMsg.MedalEntry, addMsg.CanPrint, addMsg.IsPrinted);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not MarineMedalsPanelBuiState medalsState)
            return;

        UpdateRecommendations(medalsState);
        UpdateAwardedMedals(medalsState);
    }

    private static StyleBoxFlat CreateStyleBox(Color backgroundColor) => new()
    {
        BackgroundColor = backgroundColor,
        BorderThickness = new Robust.Shared.Maths.Thickness(0),
        Padding = new Robust.Shared.Maths.Thickness(2)
    };

    private BoxContainer CreateRecommendationContainer(MarineAwardRecommendationInfo recommendation)
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
            var groupPanel = CreateRecommendationGroup(group);
            if (groupPanel != null)
            {
                _window.RecommendationsList.AddChild(groupPanel);
                _recommendationGroups[group.LastPlayerId] = groupPanel;
            }
        }
    }

    private PanelContainer? CreateRecommendationGroup(MarineRecommendationGroup group)
    {
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

        // Header: Rank, Squad (if exists), Job, Name - get from first recommendation
        var firstRec = group.Recommendations.FirstOrDefault();
        if (firstRec == null)
            return null;

        var squadPart = string.IsNullOrEmpty(firstRec.RecommendedSquad) ? null : $"({firstRec.RecommendedSquad})";
        var headerText = string.Join(" ", firstRec.RecommendedRank, squadPart, firstRec.RecommendedJob, firstRec.RecommendedName);
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

        return groupPanel;
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

    private void AddMedal(RoundCommendationEntry entry, bool canPrint, bool isPrinted)
    {
        if (_window == null)
            return;

        // Build medals info if not built yet
        if (!_medalsInfoBuilt)
        {
            BuildMedalsInfo();
            _medalsInfoBuilt = true;
        }

        var container = CreateAwardedMedalContainer(entry, canPrint, isPrinted);

        _window.ViewMedalsList.AddChild(container);
    }

    private void BuildMedalsInfo()
    {
        _medalsInfo.Clear();

        var commendationSystem = _systems.GetEntitySystem<SharedCommendationSystem>();
        var awardableMedals = commendationSystem.GetAwardableMedalIds();

        foreach (var medalId in awardableMedals)
        {
            if (!_prototype.TryIndex(medalId, out var medalProto))
                continue;

            var medalName = medalProto.Name;
            if (string.IsNullOrWhiteSpace(medalName))
                continue;

            var medalDescription = medalProto.Description ?? string.Empty;
            var medalInfo = new MedalInfo(
                new SpriteSpecifier.EntityPrototype(medalId),
                medalName,
                medalDescription
            );

            _medalsInfo[medalName] = medalInfo;
        }
    }

    private bool TryGetMedalInfo(string? prototypeId, string? name, [NotNullWhen(true)] out MedalInfo? info)
    {
        info = null;

        // First try to get by prototype ID
        if (!string.IsNullOrWhiteSpace(prototypeId))
        {
            if (_prototype.TryIndex(prototypeId, out var proto))
            {
                var medalName = proto.Name;
                if (!string.IsNullOrWhiteSpace(medalName) && _medalsInfo.TryGetValue(medalName, out info))
                {
                    return true;
                }
            }
        }

        // Fallback to name matching
        if (!string.IsNullOrWhiteSpace(name))
        {
            var entry = _medalsInfo.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, name, StringComparison.InvariantCultureIgnoreCase));

            if (entry.Value != null)
            {
                info = entry.Value;
                return true;
            }
        }

        return false;
    }

    private static string GetCommendationId(RoundCommendationEntry entry)
    {
        var commendation = entry.Commendation;
        return $"{commendation.Receiver}|{commendation.Name}|{commendation.Round}|{commendation.Text}"; // Improvised hash
    }

    private CommendationContainer CreateAwardedMedalContainer(RoundCommendationEntry entry, bool canPrint, bool isPrinted)
    {
        var spriteSystem = _systems.GetEntitySystem<SpriteSystem>();

        var container = new CommendationContainer();
        container.Title.Text = entry.Commendation.Name;
        container.Description.Text = Loc.GetString("rmc-commendation-description",
            ("receiver", entry.Commendation.Receiver),
            ("giver", entry.Commendation.Giver),
            ("text", entry.Commendation.Text));

        if (TryGetMedalInfo(entry.CommendationPrototypeId, entry.Commendation.Name, out var medalData))
        {
            var texture = spriteSystem.Frame0(medalData.Icon);
            var tooltipText = $"[bold]{medalData.Name}[/bold]\n[font size=10]{medalData.Description}[/font]";

            container.Icon.Texture = texture;
            container.Icon.Visible = true;
            container.Icon.MouseFilter = Control.MouseFilterMode.Pass;
            container.Icon.TooltipDelay = 0f;

            // Set up formatted tooltip
            container.Icon.TooltipSupplier = _ =>
            {
                var label = new RichTextLabel { MaxWidth = 400 };
                label.SetMessage(FormattedMessage.FromMarkupOrThrow(tooltipText));

                var tooltip = new Tooltip();
                tooltip.GetChild(0).Children.Clear();
                tooltip.GetChild(0).Children.Add(label);

                return tooltip;
            };
        }

        // Add print button
        var commendationId = GetCommendationId(entry);

        var printButton = new Button
        {
            HorizontalExpand = true,
            Margin = new Robust.Shared.Maths.Thickness(0, 8, 0, 0)
        };
        printButton.AddStyleClass("OpenBoth");

        if (canPrint && !isPrinted)
        {
            printButton.Text = Loc.GetString("rmc-medal-panel-print-medal");
            printButton.Disabled = false;
            printButton.OnPressed += _ =>
            {
                SendPredictedMessage(new MarineControlComputerPrintCommendationMsg { CommendationId = commendationId });
                printButton.Text = Loc.GetString("rmc-medal-panel-medal-printed");
                printButton.Disabled = true;
            };
        }
        else if (isPrinted)
        {
            printButton.Text = Loc.GetString("rmc-medal-panel-medal-printed");
            printButton.Disabled = true;
        }
        else
        {
            printButton.Text = Loc.GetString("rmc-medal-panel-cant-print");
            printButton.Disabled = true;
        }

        // Find the inner BoxContainer and add button there
        if (container.ChildCount > 0 && container.GetChild(0) is BoxContainer innerContainer)
        {
            innerContainer.AddChild(printButton);
        }
        else
        {
            container.AddChild(printButton);
        }

        return container;
    }

    private void UpdateAwardedMedals(MarineMedalsPanelBuiState state)
    {
        if (_window == null)
            return;

        // Build medals info lazily on first update
        if (!_medalsInfoBuilt)
        {
            BuildMedalsInfo();
            _medalsInfoBuilt = true;
        }

        _window.ViewMedalsList.DisposeAllChildren();

        foreach (var entry in state.AwardedMedals)
        {
            var commendationId = GetCommendationId(entry);
            var isPrinted = state.PrintedCommendationIds.Contains(commendationId);
            var canPrint = state.CanPrintCommendations;

            AddMedal(entry, canPrint, isPrinted);
        }
    }
}

