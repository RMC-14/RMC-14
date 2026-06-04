using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Access;
using Content.Shared.DoAfter;
using Content.Shared.PowerCell;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCApcComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Area;

    [DataField, AutoNetworkedField]
    public bool MainBreakerButton = true;

    [DataField, AutoNetworkedField]
    public bool ExternalPower;

    [DataField, AutoNetworkedField]
    public bool ChargeModeButton = true;

    [DataField, AutoNetworkedField]
    public RMCApcChargeStatus ChargeStatus;

    [DataField, AutoNetworkedField]
    public RMCApcChannel[] Channels = new RMCApcChannel[Enum.GetValues<RMCPowerChannel>().Length];

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField, AutoNetworkedField]
    public bool CoverLockedButton = true;

    [DataField, AutoNetworkedField]
    public RMCApcCover Cover = RMCApcCover.Closed;

    [DataField, AutoNetworkedField]
    public RMCApcElectronics Electronics = RMCApcElectronics.Secured;

    [DataField, AutoNetworkedField]
    public bool TerminalInstalled = true;

    [DataField, AutoNetworkedField]
    public bool WiresExposed;

    [DataField, AutoNetworkedField]
    public bool MainPowerWireCut;

    [DataField, AutoNetworkedField]
    public bool MainPowerWirePulsed;

    [DataField, AutoNetworkedField]
    public bool IdScannerWireCut;

    [DataField, AutoNetworkedField]
    public string CellContainerSlot = "rmc_apc_power_cell";

    [DataField, AutoNetworkedField]
    public EntProtoId<PowerCellComponent>? StartingCell;

    [DataField, AutoNetworkedField]
    public float ChargePercentage;

    [DataField, AutoNetworkedField]
    public RMCApcState State;

    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RepairTool = "Screwing";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CrowbarTool = "Prying";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CuttingTool = "Cutting";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> WeldingTool = "Welding";

    [DataField, AutoNetworkedField]
    public ProtoId<StackPrototype> CableStack = "RMCCable";

    [DataField, AutoNetworkedField]
    public int CableAmount = 10;

    [DataField, AutoNetworkedField]
    public EntProtoId CablePrototype = "RMCCableCoil10";

    [DataField, AutoNetworkedField]
    public EntProtoId ElectronicsPrototype = "CMAPCElectronics";

    [DataField, AutoNetworkedField]
    public EntProtoId FramePrototype = "CMAPCFrame";

    [DataField, AutoNetworkedField]
    public TimeSpan InstallTerminalDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan RemoveTerminalDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan InstallElectronicsDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan RemoveElectronicsDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan RepairFrameDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan DeconstructDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier CrowbarSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    [DataField]
    public SoundSpecifier DeconstructSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    [DataField]
    public SoundSpecifier ScrewdriverSound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");

    [DataField]
    public SoundSpecifier WelderSound = new SoundPathSpecifier("/Audio/Items/welder.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<AccessLevelPrototype>[] Access = ["CMAccessEngineering", "CMAccessColonyEngineering"];

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int SkillLevel = 2;
}

[Serializable, NetSerializable]
public enum RMCApcChargeStatus
{
    NotCharging,
    Charging,
    FullCharge,
}

[Serializable, NetSerializable]
public enum RMCApcButtonState
{
    Auto = 0,
    On = 1,
    Off = 2,
}

[Serializable, NetSerializable]
public enum RMCApcVisualsLayers
{
    Layer,
    Power,
    Lock,
    EquipmentChannel,
    LightingChannel,
    EnvironmentChannel,
}

[Serializable, NetSerializable]
public enum RMCApcCover
{
    Closed,
    Open,
    Removed,
}

[Serializable, NetSerializable]
public enum RMCApcElectronics
{
    Missing,
    Inserted,
    Secured,
}

[Serializable, NetSerializable]
public enum RMCApcState
{
    Working,
    WiresExposed,
    CoverOpenBattery,
    CoverOpenNoBattery,
    CoverRemovedBattery,
    CoverRemovedNoBattery,
    Broken,
    BrokenCoverRemovedBattery,
    BrokenCoverRemovedNoBattery,
    Maintenance,
}

[Serializable, NetSerializable]
public enum RMCApcChannelVisualState
{
    ManualOff,
    AutoOff,
    ManualOn,
    AutoOn,
}

[Serializable, NetSerializable]
public enum RMCApcUiKey
{
    Key,
}

[Serializable, NetSerializable]
public enum RMCApcMainPowerWireActionKey : byte
{
    Status,
    PulseCancel,
}

[Serializable, NetSerializable]
public enum RMCApcIdScannerWireActionKey : byte
{
    Status,
    PulseCancel,
}

[DataRecord]
[Serializable, NetSerializable]
public record struct RMCApcChannel(RMCApcButtonState Button, int Watts, bool On);

[Serializable, NetSerializable]
public sealed class RMCApcSetChannelBuiMsg(RMCPowerChannel channel, RMCApcButtonState state) : BoundUserInterfaceMessage
{
    public readonly RMCPowerChannel Channel = channel;
    public readonly RMCApcButtonState State = state;
}

[Serializable, NetSerializable]
public sealed class RMCApcCoverBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCApcMainBreakerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCApcChargeModeBuiMsg : BoundUserInterfaceMessage;

[RegisterComponent]
public sealed partial class RMCApcFrameComponent : Component;

[Serializable, NetSerializable]
public sealed partial class RMCApcInstallTerminalDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCApcRemoveTerminalDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCApcInstallElectronicsDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCApcRemoveElectronicsDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCApcRepairFrameDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCApcDeconstructDoAfterEvent : SimpleDoAfterEvent;
