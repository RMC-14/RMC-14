namespace Content.Shared._RMC14.Item;

[ByRefEvent]
public readonly record struct ItemCamouflageEvent(EntityUid Ent, CamouflageType Camo);
