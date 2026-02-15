namespace Content.Shared._RMC14.Construction;

[ByRefEvent]
public record struct RMCCheckTileFreeEvent(EntityUid AnchoredEntity, bool IsTileFree = false);
