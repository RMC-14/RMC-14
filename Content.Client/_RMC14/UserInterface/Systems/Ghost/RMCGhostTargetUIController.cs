using Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;
using Content.Client.UserInterface.Systems.Ghost;
using Content.Shared._RMC14.Ghost;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost;

public sealed class RMCGhostTargetUIController : UIController
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private RMCGhostTargetWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostWarpsWindowRequestedEvent>(OnGhostWarpsWindowRequested);
        SubscribeNetworkEvent<RMCGhostWarpsResponseEvent>(OnGhostWarpsResponse);
    }

    private void OnGhostWarpsWindowRequested(GhostWarpsWindowRequestedEvent args)
    {
        args.Handled = true;

        var window = EnsureWindow();
        window.Populate();

        if (!window.IsOpen)
            window.OpenCentered();

        _net.SendSystemNetworkMessage(new RMCGhostWarpsRequestEvent());
    }

    private void OnGhostWarpsResponse(RMCGhostWarpsResponseEvent msg, EntitySessionEventArgs args)
    {
        var window = EnsureWindow();
        window.UpdateSections(msg.Sections);
        window.Populate();
    }

    private void OnWarpClicked(NetEntity target)
    {
        _net.SendSystemNetworkMessage(new RMCGhostWarpToTargetRequestEvent(target));
    }

    private void OnGhostnadoClicked()
    {
        _net.SendSystemNetworkMessage(new RMCGhostnadoRequestEvent());
    }

    private RMCGhostTargetWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new RMCGhostTargetWindow();
        _window.WarpClicked += OnWarpClicked;
        _window.OnGhostnadoClicked += OnGhostnadoClicked;
        return _window;
    }
}
