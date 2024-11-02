using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[DataRecord]
[Serializable, NetSerializable]
public record struct OverwatchSupplyDropLocation(int Longitude, int Latitude, string Comment);
