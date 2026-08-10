using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[DataRecord]
public readonly record struct RMCPowerNetworkKey(EntityUid Map, string PowerNet);

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCPowerNetworkStats(
    float AvailableGeneration,
    float Generation,
    float Demand,
    float Delivered,
    float Deficit,
    float Surplus,
    float StorageCharge,
    float StorageDischarge);

[Serializable, NetSerializable]
public enum RMCPowerSourceScope
{
    Network,
    Area,
}

[Serializable, NetSerializable]
public enum RMCPowerStorageInputState
{
    Off,
    Partial,
    Full,
}

public readonly record struct RMCPowerNetworkUpdatedEvent(
    RMCPowerNetworkKey Key,
    RMCPowerNetworkStats Stats);
