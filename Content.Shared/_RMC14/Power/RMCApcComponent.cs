using Content.Shared.PowerCell;
using Content.Shared.Tools;
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
    public bool MainBreakerButton;

    [DataField, AutoNetworkedField]
    public bool ExternalPower;

    [DataField, AutoNetworkedField]
    public bool ChargeModeButton;

    [DataField, AutoNetworkedField]
    public RMCApcChargeStatus ChargeStatus;

    [DataField, AutoNetworkedField]
    public RMCApcChannel[] Channels = new RMCApcChannel[Enum.GetValues<RMCPowerChannel>().Length];

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public bool CoverLockedButton;

    [DataField, AutoNetworkedField]
    public string CellContainerSlot = "rmc_apc_power_cell";

    [DataField, AutoNetworkedField]
    public EntProtoId<PowerCellComponent>? StartingCell = "RMCPowerCellHigh";

    [DataField, AutoNetworkedField]
    public float ChargePercentage;

    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RepairTool = "Screwing";
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
}

[Serializable, NetSerializable]
public enum RMCApcVisuals
{
    Working,
    Broken,
}

[Serializable, NetSerializable]
public enum RMCApcUiKey
{
    Key,
}

[DataRecord]
[Serializable, NetSerializable]
public record struct RMCApcChannel(RMCApcButtonState Button, int Watts, bool On);

[Serializable, NetSerializable]
public sealed class RMCApcSetChannelBuiMsg(RMCPowerChannel channel, bool on) : BoundUserInterfaceMessage
{
    public readonly RMCPowerChannel Channel = channel;
    public readonly bool On = on;
}
