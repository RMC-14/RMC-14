using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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

    /// <summary>
    /// Temperature in Kelvin
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CryoCellTemperature = 115f;

    /// <summary>
    /// Temperature in Kelvin
    /// </summary>
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
    public ProtoId<RadioChannelPrototype> ReleaseNoticeRadioChannel = "MarineMedical";

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier BeepBeep = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public SoundSpecifier Ping = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    #region UI Fields

    [AutoNetworkedField]
    public NetEntity? UIOccupant;

    [AutoNetworkedField]
    public string? UIOccupantName;

    [AutoNetworkedField]
    public CryoCellOccupantMobState UIOccupantState = CryoCellOccupantMobState.None;

    [AutoNetworkedField]
    public float UIHealth;

    [AutoNetworkedField]
    public float UIMaxHealth;

    [AutoNetworkedField]
    public float UIBruteLoss;

    [AutoNetworkedField]
    public float UIBurnLoss;

    [AutoNetworkedField]
    public float UIToxinLoss;

    [AutoNetworkedField]
    public float UIOxygenLoss;

    [AutoNetworkedField]
    public float UIBodyTemperature;

    [AutoNetworkedField]
    public bool UIIsBeakerLoaded;

    [AutoNetworkedField]
    public CryoCellBeakerReagent[] UIBeakerContents = [];

    #endregion
}

[Serializable, NetSerializable]
public readonly record struct CryoCellBeakerReagent(string Name, float Volume);

[Serializable, NetSerializable]
public enum CryoCellUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum CryoCellVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum CryoCellVisualState : byte
{
    OffEmpty = 0,
    OffOccupied = 1,
    OnEmpty = 2,
    OnOccupied = 3
}

[Serializable, NetSerializable]
public enum CryoCellVisualLayers
{
    Base
}

[Serializable, NetSerializable]
public enum CryoCellOccupantMobState : byte
{
    None = 0,
    Alive = 1,
    Critical = 2,
    Dead = 3
}

[Serializable, NetSerializable]
public sealed class CryoCellTogglePowerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellToggleAutoEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellEjectBeakerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellToggleNotifyBuiMsg : BoundUserInterfaceMessage;
