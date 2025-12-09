namespace Content.Shared._RMC14.Xenonids.Weeds;

[ByRefEvent]
public readonly record struct AfterEntityWeedingEvent(EntityUid Weeds, EntityUid CoveredEntity);
