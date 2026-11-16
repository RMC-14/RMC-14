using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hive;

[ByRefEvent]
[Serializable, NetSerializable]
public readonly record struct RenegadeDefectionChosenEvent(bool Defect);
