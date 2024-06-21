using Content.Shared.Inventory;

namespace Content.Shared._CM14.Medical.HUD.Events;

[ByRefEvent]
public record struct HolocardScanEvent(bool CanScan, SlotFlags TargetSlots) : IInventoryRelayEvent;
