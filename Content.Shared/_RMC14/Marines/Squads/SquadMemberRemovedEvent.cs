namespace Content.Shared._RMC14.Marines.Squads;

[ByRefEvent]
public readonly record struct SquadMemberRemovedEvent(Entity<SquadTeamComponent> Squad, EntityUid Member);
