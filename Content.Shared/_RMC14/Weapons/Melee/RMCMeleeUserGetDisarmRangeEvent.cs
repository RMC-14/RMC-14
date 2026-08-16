namespace Content.Shared._RMC14.Weapons.Melee;

[ByRefEvent]
public record struct RMCMeleeUserGetDisarmRangeEvent(EntityUid? Target, float Range);
