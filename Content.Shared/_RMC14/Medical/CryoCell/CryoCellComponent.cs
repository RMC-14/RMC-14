using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCryoCellSystem))]
public sealed partial class CryoCellComponent : Component
{
    [DataField]
    public string OccupantId = "cryo_cell";

    [DataField]
    public string BeakerSlot = "beakerSlot";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    // Temperatures in Kelvin
    [DataField, AutoNetworkedField]
    public float CryoCellTemperature = 115f;

    [DataField, AutoNetworkedField]
    public float BodyTempCryoLiquidThreshold = 210f;

    [DataField, AutoNetworkedField]
    public bool IsPoweredOn;

    [DataField, AutoNetworkedField]
    public bool AutoEject;

    [DataField, AutoNetworkedField]
    public bool ReleaseNotice;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    [DataField]
    public float BeakerTransferAmount = 5f;

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 10 * GLOBAL_STATUS_MULTIPLIER = 200 deciseconds
    /// </summary>
    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(20);

    /// <summary>
    /// 10 * GLOBAL_STATUS_MULTIPLIER = 200 deciseconds
    /// </summary>
    [DataField]
    public TimeSpan UnconsciousDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public ProtoId<RadioChannelPrototype> ReleaseNoticeAnnouncement = "MarineMedical";

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier BeepBeep = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier Ping = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
