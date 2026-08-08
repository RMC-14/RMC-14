using Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;
using Content.Shared._RMC14.Ghost;
using Content.Shared.Ghost;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using ClientGhostSystem = Content.Client.Ghost.GhostSystem;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost;

public sealed class RMCGhostTargetUIController : UIController, IOnSystemChanged<ClientGhostSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private readonly RMCGhostTargetRequestState _requests = new();
    private RMCGhostTargetWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

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

    public void OpenWindow()
    {
        var window = EnsureWindow();

        if (!window.IsOpen)
            window.OpenCentered();

        RequestWarps();
    }

    private void OnGhostWarpsResponse(RMCGhostWarpsResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_requests.TryComplete(msg.RequestId, out var refreshQueued))
            return;

        if (_window?.IsOpen == true)
            _window.UpdateContent(msg.Self, msg.Targets, msg.Sections);

        if (!refreshQueued)
            return;

        if (_window?.IsOpen == true)
            RequestWarps();
    }

    private void OnWarpClicked(NetEntity target)
    {
        _net.SendSystemNetworkMessage(new RMCGhostWarpToTargetRequestEvent(target));
    }

    private void RequestWarps()
    {
        if (_requests.Request() is not { } requestId)
            return;

        _net.SendSystemNetworkMessage(new RMCGhostWarpsRequestEvent(requestId));
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        _requests.CancelQueued();
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
