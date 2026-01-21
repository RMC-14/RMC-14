namespace Content.Shared._RMC14.Chemistry;

/// <summary>
/// Event to start the vomit process (vomit() proc).
/// This starts the delayed vomit sequence with nausea, warning, then actual vomit.
/// </summary>
[ByRefEvent]
public readonly record struct RMCVomitEvent(EntityUid Target);

/// <summary>
/// Event to perform the actual vomit immediately (do_vomit() proc).
/// </summary>
[ByRefEvent]
public readonly record struct RMCDoVomitEvent(
    EntityUid Target,
    TimeSpan StunDuration,
    float HungerLoss,
    float ToxinHeal
);
