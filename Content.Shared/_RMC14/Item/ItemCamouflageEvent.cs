namespace Content.Shared._RMC14.Item;

[ByRefEvent]
public readonly record struct ItemCamouflageEvent(EntityUid Old, EntityUid New, bool ReplaceOverride = false);
