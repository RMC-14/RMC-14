using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared._CM14.Medical.HUD.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CM14.Medical.HUD.Systems;

public sealed class HolocardScannerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InventoryComponent, HolocardScanEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<HolocardScannerComponent>>(_inventory.RelayEvent);

        SubscribeLocalEvent<HolocardScannerComponent, HolocardScanEvent>(OnHolocardScanAttempt);
        SubscribeLocalEvent<HolocardScannerComponent, InventoryRelayedEvent<HolocardScanEvent>>(OnRelayedHolocardScanAttempt);
    }

    private void OnHolocardScanAttempt(Entity<HolocardScannerComponent> ent, ref HolocardScanEvent args)
    {
        args.CanScan = true;
    }

    private void OnRelayedHolocardScanAttempt(Entity<HolocardScannerComponent> ent, ref InventoryRelayedEvent<HolocardScanEvent> args)
    {
        args.Args.CanScan = true;
    }
}
