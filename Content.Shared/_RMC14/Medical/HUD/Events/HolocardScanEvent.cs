using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Medical.HUD.Events;

[ByRefEvent]
public record struct HolocardScanEvent(bool CanScan, SlotFlags TargetSlots) : IInventoryRelayEvent;
