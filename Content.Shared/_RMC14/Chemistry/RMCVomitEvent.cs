namespace Content.Shared._RMC14.Chemistry;

/// <summary>
/// Event to start the delayed vomit process. vomit() proc
/// Vomit sequence: nausea -> warning -> actual vomit
/// </summary>
[ByRefEvent]
public readonly record struct RMCVomitEvent(
    EntityUid Target,
    float HungerLoss = -40f,
    float ToxinHeal = 3f
);

/// <summary>
/// Event to perform the actual vomit immediately. do_vomit() proc
/// </summary>
[ByRefEvent]
public readonly record struct RMCDoVomitEvent(
    EntityUid Target,
    float HungerLoss = -40f,
    float ToxinHeal = 3f
);
