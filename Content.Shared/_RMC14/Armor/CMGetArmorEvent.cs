using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Armor;

[ByRefEvent]
public record struct CMGetArmorEvent(SlotFlags TargetSlots, int Armor = 0, int Bio = 0, int FrontalArmor = 0) : IInventoryRelayEvent;
