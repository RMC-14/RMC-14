using Content.Shared.Inventory.Events;

namespace Content.Shared._RMC14.Waypoint;

public sealed class RMCTrackerAlertGranterSystem : EntitySystem
{
    [Dependency] private readonly TrackerAlertSystem _trackerAlert = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTrackerAlertGranterClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<RMCTrackerAlertGranterClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<RMCTrackerAlertGranterClothingComponent> ent, ref GotEquippedEvent args)
    {
        var tracker = EnsureComp<RMCTrackerAlertComponent>(args.Equipee);
        foreach (var (proto, alert) in ent.Comp.Alerts)
        {
            if (!tracker.Alerts.TryAdd(proto, alert))
                Log.Error($"AlertPrototype {proto} already added.");
        }
    }

    private void OnGotUnequipped(Entity<RMCTrackerAlertGranterClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<RMCTrackerAlertComponent>(args.Equipee, out var tracker))
            return;

        _trackerAlert.RemoveAlerts((args.Equipee, tracker), ent.Comp.Alerts.Keys);
    }
}
