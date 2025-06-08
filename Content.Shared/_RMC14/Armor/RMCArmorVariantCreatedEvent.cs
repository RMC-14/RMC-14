namespace Content.Shared._RMC14.Armor;

[ByRefEvent]
public readonly record struct RMCArmorVariantCreatedEvent(EntityUid Old, EntityUid New);
