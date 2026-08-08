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

    // Temperatures in Kelvin
    [DataField, AutoNetworkedField]
    public float CryoCellTemperature = 0f;

    [DataField, AutoNetworkedField]
    public bool IsPoweredOn;

    [DataField, AutoNetworkedField]
    public bool AutoEject;

    [DataField, AutoNetworkedField]
    public bool Notice;

    [DataField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    [DataField]
    public float BeakerTransferAmount = 5f;

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    // amount * GLOBAL_STATUS_MULTIPLIER
    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(20);

    // amount * GLOBAL_STATUS_MULTIPLIER
    [DataField]
    public TimeSpan UnconsciousDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier HealingCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}
