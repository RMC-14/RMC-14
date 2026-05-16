using Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;
using Content.Client.UserInterface.Systems.Ghost;
using Content.Shared._RMC14.Ghost;
using Content.Shared.Ghost;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using ClientGhostSystem = Content.Client.Ghost.GhostSystem;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost;

public sealed class RMCGhostTargetUIController : UIController, IOnSystemChanged<ClientGhostSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private RMCGhostTargetWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostWarpsWindowRequestedEvent>(OnGhostWarpsWindowRequested);
        SubscribeNetworkEvent<RMCGhostWarpsResponseEvent>(OnGhostWarpsResponse);
    }

    public void OnSystemLoaded(ClientGhostSystem system)
    {
        system.PlayerDetached += CloseWindow;
        system.PlayerRemoved += OnPlayerRemoved;
    }

    public void OnSystemUnloaded(ClientGhostSystem system)
    {
        system.PlayerDetached -= CloseWindow;
        system.PlayerRemoved -= OnPlayerRemoved;
    }

    private void OnGhostWarpsWindowRequested(GhostWarpsWindowRequestedEvent args)
    {
        args.Handled = true;

        var window = EnsureWindow();

        if (!window.IsOpen)
            window.OpenCentered();

        RequestWarps();
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

    private void RequestWarps()
    {
        _net.SendSystemNetworkMessage(new RMCGhostWarpsRequestEvent());
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private RMCGhostTargetWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new RMCGhostTargetWindow();
        _window.WarpClicked += OnWarpClicked;
        _window.OnRefreshClicked += RequestWarps;
        return _window;
    }
}
