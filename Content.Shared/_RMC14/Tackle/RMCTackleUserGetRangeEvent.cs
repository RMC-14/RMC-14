namespace Content.Shared._RMC14.Tackle; // adjust namespace to match your project

[ByRefEvent]
public record struct RMCGetTackleRangeEvent(EntityUid? Target, float Range);
