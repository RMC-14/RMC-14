using Content.Shared._RMC14.Areas;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;


namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapMarineAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantTacMapAlertComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantTacMapAlertComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<TacMapMarineAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TacMapMarineAlertComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<TacMapMarineAlertComponent, MoveEvent>(OnMove);
    }
    private void OnGotEquipped(Entity<GrantTacMapAlertComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        EnsureComp<TacMapMarineAlertComponent>(args.Equipee);
    }
    private void OnGotUnequipped(Entity<GrantTacMapAlertComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;
        if (!_inv.TryGetInventoryEntity<GrantTacMapAlertComponent>(args.Equipee, out _))
            RemCompDeferred<TacMapMarineAlertComponent>(args.Equipee);
    }
    private void OnMapInit(Entity<TacMapMarineAlertComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(ent))));
    }
    private void OnMove(Entity<TacMapMarineAlertComponent> ent, ref MoveEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(ent))));
    }
    private void OnRemove(Entity<TacMapMarineAlertComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private string GetAreaName(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var _, out var areaProto))
            return Loc.GetString("rmc-tacmap-alert-no-area");

        return areaProto.Name;
    }
}
