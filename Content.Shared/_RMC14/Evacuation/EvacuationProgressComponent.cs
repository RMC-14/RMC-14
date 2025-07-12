using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class EvacuationProgressComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public bool DropShipCrashed;

    [DataField, AutoNetworkedField]
    public bool StartAnnounced;

    [DataField, AutoNetworkedField]
    public double Progress;

    [DataField, AutoNetworkedField]
    public double Required = 100;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public int AnnounceEvery = 25;

    [DataField, AutoNetworkedField]
    public int NextAnnounce;

    [DataField]
    public Dictionary<EntityUid, bool> LastPower = new();
}
