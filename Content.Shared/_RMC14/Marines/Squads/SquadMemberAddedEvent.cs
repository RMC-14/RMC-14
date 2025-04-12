namespace Content.Shared._RMC14.Marines.Squads;

[ByRefEvent]
public readonly record struct SquadMemberAddedEvent(Entity<SquadTeamComponent> Squad, EntityUid Member);
