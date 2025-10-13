namespace Content.Shared._RMC14.Chemistry.Events;

[ByRefEvent]
public readonly record struct RMCVomitEvent(
    EntityUid Target,
    float ThirstAmount = -8f,
    float HungerAmount = -8f
);
