using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapSnapshotComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, TacticalMapBlip> Blips = new();

    [ViewVariables]
    public HashSet<NetEntity> AlwaysVisible = new();

    [DataField, AutoNetworkedField]
    public TimeSpan TakenAt;
}
