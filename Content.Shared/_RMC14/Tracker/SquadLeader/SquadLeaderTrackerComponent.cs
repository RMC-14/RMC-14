using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class SquadLeaderTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "SquadTracker";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery;
}
