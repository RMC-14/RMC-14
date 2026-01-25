namespace Content.Shared._RMC14.Weapons.Melee;

[ByRefEvent]
public record struct RMCMeleeUserGetRangeEvent(EntityUid? Target, float Range);
