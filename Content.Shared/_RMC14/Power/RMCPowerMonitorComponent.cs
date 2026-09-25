using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPowerMonitorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Connected;

    [DataField, AutoNetworkedField]
    public string PowerNet = string.Empty;

    [DataField, AutoNetworkedField]
    public RMCPowerNetworkStats Stats;

    [DataField, AutoNetworkedField]
    public RMCPowerMonitorStorage[] Storages = [];

    [DataField, AutoNetworkedField]
    public RMCPowerMonitorApc[] Apcs = [];
}

[Serializable, NetSerializable]
public readonly record struct RMCPowerMonitorStorage(
    string Name,
    float Charge,
    float MaxCharge,
    bool InputEnabled,
    RMCPowerStorageInputState InputState,
    float InputLimit,
    float Input,
    bool OutputEnabled,
    float OutputLimit,
    float Output);

[Serializable, NetSerializable]
public readonly record struct RMCPowerMonitorApc(
    string Area,
    RMCApcChannelVisualState Equipment,
    RMCApcChannelVisualState Lighting,
    RMCApcChannelVisualState Environment,
    float Requested,
    float Delivered,
    bool HasCell,
    RMCApcChargeStatus ChargeStatus,
    float CellCharge);

[Serializable, NetSerializable]
public enum RMCPowerMonitorUiKey
{
    Key,
}
