namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[ByRefEvent]
public record struct XenoSecreteStructureAttemptEvent(EntityUid? TargetToReplace = null, bool Cancelled = false);
