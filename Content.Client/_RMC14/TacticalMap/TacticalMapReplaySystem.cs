using Content.Shared._RMC14.TacticalMap;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapReplaySystem : EntitySystem
{
    private TacticalMapReplayWindow? _window;
    private string? _preferredMapId;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TacticalMapReplayDataEvent>(OnReplayData);
    }

    public void RequestReplay(string? mapId)
    {
        _preferredMapId = mapId;
        RaiseNetworkEvent(new TacticalMapReplayRequestEvent(mapId));
    }

    private void OnReplayData(TacticalMapReplayDataEvent msg)
    {
        if (_window == null || _window.Disposed)
            _window = new TacticalMapReplayWindow();

        _window.SetReplayData(msg.Maps, _preferredMapId);
        _window.OpenCentered();
        _preferredMapId = null;
    }
}
