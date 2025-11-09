namespace Content.Shared._RMC14.NewPlayer;

/// <summary>
///     Event raised when a player has 1 or less hours in a job.
/// </summary>
[ByRefEvent]
    public readonly record struct NewToJobEvent(
        EntityUid player,
        string? jobInfo,
        string jobName
);
