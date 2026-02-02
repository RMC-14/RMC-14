using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Commendations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Commendations;

public sealed class CommendationsManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private CommendationsWindow? _receivedWindow;
    private CommendationsWindow? _givenWindow;

    private readonly List<Commendation> _commendationsReceived = new();
    private readonly List<Commendation> _commendationsGiven = new();

    private sealed record MedalInfo(SpriteSpecifier Icon, string Name, string Description);
    private readonly Dictionary<string, MedalInfo> _medalsInfo = new();
    private bool _medalsInfoBuilt = false;

    public void PostInject()
    {
        _net.RegisterNetMessage<CommendationsMsg>(OnCommendations);
    }

    private void OnCommendations(CommendationsMsg message)
    {
        _commendationsReceived.Clear();
        _commendationsReceived.AddRange(message.CommendationsReceived.OrderByDescending(c => c.Round));

        _commendationsGiven.Clear();
        _commendationsGiven.AddRange(message.CommendationsGiven.OrderByDescending(c => c.Round));
    }

    private void BuildMedalsInfo()
    {
        _medalsInfo.Clear();

        var commendationSystem = _systems.GetEntitySystem<CommendationSystem>();
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

    private bool TryGetMedalInfo(string name, [NotNullWhen(true)] out MedalInfo? info)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            info = null;
            return false;
        }

        // Search case-insensitively
        var entry = _medalsInfo.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, name, StringComparison.InvariantCultureIgnoreCase));

        if (entry.Value == null)
        {
            info = null;
            return false;
        }

        info = entry.Value;
        return true;
    }

    private void OpenWindow(ref CommendationsWindow? window, Action onClose, List<Commendation> commendations)
    {
        if (window != null)
        {
            window.MoveToFront();
            return;
        }

        // Build medals info lazily on first window open
        if (!_medalsInfoBuilt)
        {
            BuildMedalsInfo();
            _medalsInfoBuilt = true;
        }

        window = new CommendationsWindow();
        window.OnClose += onClose;
        window.OpenCentered();

        var spriteSystem = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();

        foreach (var commendation in commendations)
        {
            var container = new CommendationContainer();
            container.Title.Text = Loc.GetString("rmc-commendation-title",
                ("round", commendation.Round),
                ("name", commendation.Name));
            container.Description.Text = Loc.GetString("rmc-commendation-description",
                ("receiver", commendation.Receiver),
                ("giver", commendation.Giver),
                ("text", commendation.Text));

            if (TryGetMedalInfo(commendation.Name, out var medalInfo))
            {
                var texture = spriteSystem.Frame0(medalInfo.Icon);
                var tooltipText = $"[bold]{medalInfo.Name}[/bold]\n[font size=10]{medalInfo.Description}[/font]";

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

            // Add container to appropriate tab based on commendation type
            window.AddCommendation(commendation.Type, container);
        }
    }

    public void OpenReceivedWindow()
    {
        OpenWindow(ref _receivedWindow, () => _receivedWindow = null, _commendationsReceived);
    }

    public void OpenGivenWindow()
    {
        OpenWindow(ref _givenWindow, () => _givenWindow = null, _commendationsGiven);
    }
}
