using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Tracker.Xeno;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveTrackerSystem))]
public sealed partial class HiveTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "HiveTracker";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery;
}
