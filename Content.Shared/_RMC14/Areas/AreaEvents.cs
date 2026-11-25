namespace Content.Shared._RMC14.Areas;

[ByRefEvent]
public readonly record struct AreaGridRenderedEvent;

[ByRefEvent]
public readonly record struct RoofingEntityChangedEvent(EntityUid Roof);
