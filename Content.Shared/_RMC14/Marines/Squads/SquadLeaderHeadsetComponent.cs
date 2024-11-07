using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadLeaderHeadsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = ["MarineJTAC", "MarineCommand"];

    [DataField, AutoNetworkedField]
    public EntityUid Leader;
}
