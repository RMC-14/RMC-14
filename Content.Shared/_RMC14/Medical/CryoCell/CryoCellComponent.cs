using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCryoCellSystem))]
public sealed partial class CryoCellComponent : Component
{
    [DataField]
    public string ContainerId = "cryo_cell";

    [DataField]
    public string BeakerSlot = "beakerSlot";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    [DataField, AutoNetworkedField]
    public bool On;

    [DataField, AutoNetworkedField]
    public bool AutoEject;

    [DataField, AutoNetworkedField]
    public bool ReleaseNotice;

    [DataField, AutoNetworkedField]
    public float Temperature;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/scanning_pod1.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float TemperatureDropPerTick = 1.0f;

    [DataField, AutoNetworkedField]
    public float HealBrutePerTick = 1.0f;

    [DataField, AutoNetworkedField]
    public float HealBurnPerTick = 1.0f;

    [DataField, AutoNetworkedField]
    public float HealToxinPerTick = 0.5f;
}
