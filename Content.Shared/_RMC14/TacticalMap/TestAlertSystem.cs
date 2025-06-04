using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared._RMC14.Areas;
using Content.Shared.Coordinates;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TestAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    private readonly Queue<Entity<GrantTestAlertComponent>> _testAlertQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantTestAlertComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantTestAlertComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<GrantTestAlertComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        _alerts.ShowAlert(args.Equipee, "RMCTestAlert", dynamicMessage: GetAreaCapabilities(args.Equipee));
    }

    private void OnGotUnequipped(Entity<GrantTestAlertComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (!_inv.TryGetInventoryEntity<GrantTestAlertComponent>(args.Equipee, out _))
            _alerts.ClearAlert(args.Equipee, "RMCTestAlert");
    }

    private string GetAreaCapabilities(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var area, out var _))
            return Loc.GetString("rmc-tacmap-alert-no-area");

        if (!TryComp<AreaComponent>(area, out var areaComp))
            return Loc.GetString("rmc-tacmap-alert-no-area");

        var capabilities = new List<string>();

        if (areaComp.CAS)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-cas"));
        if (areaComp.Fulton)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-fulton"));
        if (areaComp.MortarPlacement)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-mortar-placement"));
        if (areaComp.MortarFire)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-mortar-fire"));
        if (areaComp.Lasing)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-lasing"));
        if (areaComp.Medevac)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-medevac"));
        if (areaComp.OB)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-ob"));
        if (areaComp.SupplyDrop)
            capabilities.Add(Loc.GetString("rmc-tacmap-alert-capability-supply-drop"));

        if (capabilities.Count == 0)
            return Loc.GetString("rmc-tacmap-alert-no-capabilities");

        return string.Join(", ", capabilities);
    }

    public override void Update(float frameTime)
    {
        if (_testAlertQueue.Count == 0)
            return;

        var time = _timing.CurTime;
        while (_testAlertQueue.TryDequeue(out var ent))
        {
            if (_timing.CurTime >= time + TimeSpan.FromMilliseconds(100))
                return;

            if (TerminatingOrDeleted(ent))
                continue;

            _alerts.ShowAlert(ent, "RMCTestAlert", dynamicMessage: GetAreaCapabilities(ent));
        }
    }
}
