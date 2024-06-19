using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared.Inventory;

namespace Content.Shared._CM14.Medical.HUD.Systems;

public sealed class HolocardScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        //base.Initialize();

        SubscribeLocalEvent<HolocardScannerComponent, HolocardScanEvent>(OnHolocardScanAttempt);
        SubscribeLocalEvent<HolocardScannerComponent, InventoryRelayedEvent<HolocardScanEvent>>((e, c, ev) => OnHolocardScanAttempt(e, c, ev.Args));
    }

    private void OnHolocardScanAttempt(EntityUid eid, HolocardScannerComponent component, HolocardScanEvent args)
    {
        args.CanScan = true;
    }
}

public sealed class HolocardScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
