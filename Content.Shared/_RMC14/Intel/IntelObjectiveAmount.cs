using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel;

[Serializable, NetSerializable]
[DataRecord]
public record struct IntelObjectiveAmount(int Current, int Total);
