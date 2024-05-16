using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Destination;

    [DataField, AutoNetworkedField]
    public bool Crashed;

    [DataField, AutoNetworkedField]
    public TimeSpan AutoRecallRoundDelay = TimeSpan.FromMinutes(30);

    [DataField, AutoNetworkedField]
    public TimeSpan AutoRecallTime = TimeSpan.FromMinutes(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan AutoRecallAt;

    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> AnnounceAutoRecallIn = "MarineCommon";
}
