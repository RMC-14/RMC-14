namespace Content.Shared._RMC14.Armor.Plates;

[ByRefEvent]
public readonly record struct RMCArmorPlateAppliedEvent(EntityUid Plate);

[ByRefEvent]
public readonly record struct RMCArmorPlateRemovedEvent(EntityUid Plate);
