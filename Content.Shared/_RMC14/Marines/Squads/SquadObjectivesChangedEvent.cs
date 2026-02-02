namespace Content.Shared._RMC14.Marines.Squads;

/// <summary>
/// Raised when squad objectives are changed (set or removed).
/// </summary>
[ByRefEvent]
public readonly record struct SquadObjectivesChangedEvent(Entity<SquadTeamComponent> Squad);

