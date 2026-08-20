using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
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
    [DataField]
    public float CryoCellTemperature = 115f;

    [DataField]
    public float BodyTempCryoLiquidThreshold = 210f;

    [DataField, AutoNetworkedField]
    public bool IsPoweredOn;

    [DataField, AutoNetworkedField]
    public bool AutoEject;

    [DataField, AutoNetworkedField]
    public bool ReleaseNotice;

    [DataField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

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
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier HealingCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [AutoNetworkedField]
    public string? OccupantName;

    [AutoNetworkedField]
    public CryoCellOccupantMobState OccupantState;

    [AutoNetworkedField]
    public float Health;

    [AutoNetworkedField]
    public float MaxHealth;

    [AutoNetworkedField]
    public float BruteLoss;

    [AutoNetworkedField]
    public float BurnLoss;

    [AutoNetworkedField]
    public float ToxinLoss;

    [AutoNetworkedField]
    public float OxyLoss;

    [AutoNetworkedField]
    public float BodyTemperature;

    [AutoNetworkedField]
    public bool IsBeakerLoaded;

    [AutoNetworkedField]
    public CryoCellBeakerReagent[] BeakerContents = [];
}
