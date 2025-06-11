using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Tracker.Xeno;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveTrackerSystem))]
public sealed partial class HiveTrackerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TrackerModePrototype> DefaultMode;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<TrackerModePrototype>> TrackerModes = new();

    [DataField, AutoNetworkedField]
    public ProtoId<TrackerModePrototype> Mode;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
