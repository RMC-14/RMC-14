using Content.Client._RMC14.TacticalMap.UI;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.GameTicking;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapReplaySystem : EntitySystem
{
    private TacticalMapReplayWindow? _window;
    private int _requestId = -1;

    public override void Initialize()
    {
        SubscribeNetworkEvent<TacticalMapReplayHeaderEvent>(OnHeader);
        SubscribeNetworkEvent<TacticalMapReplayChunkEvent>(OnChunk);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CloseWindow();
    }

    private void OnHeader(TacticalMapReplayHeaderEvent ev)
    {
        _requestId = ev.RequestId;
        if (_window == null || _window.Disposed)
        {
            _window = new TacticalMapReplayWindow();
            _window.OnClose += OnWindowClosed;
        }

        _window.SetReplay(ev.Maps);
        if (!_window.IsOpen)
            _window.OpenCentered();
    }

    private void OnChunk(TacticalMapReplayChunkEvent ev)
    {
        if (ev.RequestId != _requestId || _window == null || _window.Disposed)
            return;

        _window.AddFrames(ev.MapIndex, ev.Frames);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        CloseWindow();
    }

    private void OnWindowClosed()
    {
        _window?.Orphan();
        _window = null;
        _requestId = -1;
    }

    private void CloseWindow()
    {
        _window?.Close();
    }
}
