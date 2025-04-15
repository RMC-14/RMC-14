namespace Content.Shared._RMC14.Xenonids.Hive;

[ByRefEvent]
public readonly record struct BurrowedLarvaChangedEvent(Entity<HiveComponent> Hive);
