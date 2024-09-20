using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, TacticalMapBlip> Blips = new();

    [DataField, AutoNetworkedField]
    public List<TacticalMapLine> Lines = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAnnounceAt;
}
