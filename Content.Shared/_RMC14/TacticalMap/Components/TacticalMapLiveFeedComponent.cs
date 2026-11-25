using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapLiveFeedComponent : Component
{
    [ViewVariables]
    public EntityUid? Channel;

    [ViewVariables]
    public Dictionary<NetEntity, TacticalMapBlip> Blips = new();

    [ViewVariables]
    public Dictionary<NetEntity, GameTick> Modified = new();

    [ViewVariables]
    public HashSet<NetEntity> Revealed = new();
}

[Serializable, NetSerializable]
public sealed class TacticalMapLiveFeedState(NetEntity channel, Dictionary<NetEntity, TacticalMapBlip> blips) : ComponentState
{
    public readonly NetEntity Channel = channel;
    public readonly Dictionary<NetEntity, TacticalMapBlip> Blips = blips;
}

[Serializable, NetSerializable]
public sealed class TacticalMapLiveFeedDeltaState(
    NetEntity channel,
    Dictionary<NetEntity, TacticalMapBlip> modified,
    HashSet<NetEntity> all) : ComponentState, IComponentDeltaState<TacticalMapLiveFeedState>
{
    public readonly NetEntity Channel = channel;
    public readonly Dictionary<NetEntity, TacticalMapBlip> Modified = modified;
    public readonly HashSet<NetEntity> All = all;

    public void ApplyToFullState(TacticalMapLiveFeedState state)
    {
        foreach (var key in state.Blips.Keys)
        {
            if (!All.Contains(key))
                state.Blips.Remove(key);
        }

        foreach (var (key, blip) in Modified)
        {
            state.Blips[key] = blip;
        }
    }

    public TacticalMapLiveFeedState CreateNewFullState(TacticalMapLiveFeedState state)
    {
        var blips = new Dictionary<NetEntity, TacticalMapBlip>(All.Count);
        foreach (var key in All)
        {
            if (Modified.TryGetValue(key, out var modified))
                blips[key] = modified;
            else if (state.Blips.TryGetValue(key, out var existing))
                blips[key] = existing;
        }

        return new TacticalMapLiveFeedState(Channel, blips);
    }
}
