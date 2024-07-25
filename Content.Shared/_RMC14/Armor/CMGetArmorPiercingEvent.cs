namespace Content.Shared._RMC14.Armor;

[ByRefEvent]
public record struct CMGetArmorPiercingEvent(EntityUid Target, int Piercing = 0);
