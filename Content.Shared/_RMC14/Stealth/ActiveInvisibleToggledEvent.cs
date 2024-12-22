using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Stealth;

[ByRefEvent]
public record struct ActiveInvisibleToggledEvent(
    bool enabled
);