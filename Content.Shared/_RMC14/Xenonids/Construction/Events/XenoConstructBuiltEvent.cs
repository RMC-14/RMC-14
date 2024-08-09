using Content.Shared._RMC14.Xenonids.Hive;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

/// <summary>
/// Raised on a xeno construct after it gets built and has a hive assigned.
/// </summary>
[ByRefEvent]
public readonly record struct XenoConstructBuiltEvent(EntityUid Builder, Entity<HiveComponent> Hive);
