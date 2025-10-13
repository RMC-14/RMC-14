namespace Content.Shared._RMC14.Chemistry;

[ByRefEvent]
public readonly record struct RMCVomitEvent(
    EntityUid Target,
    float ThirstAmount = -8f,
    float HungerAmount = -8f
);
