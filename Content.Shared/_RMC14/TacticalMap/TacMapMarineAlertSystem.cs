using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapMarineAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private readonly Queue<Entity<TacMapMarineAlertComponent>> _marineAlertQueue = new();

    private TimeSpan _maxProcessTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantTacMapAlertComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantTacMapAlertComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<TacMapMarineAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TacMapMarineAlertComponent, ComponentRemove>(OnRemove);

        Subs.CVar(_config, RMCCVars.RMCMaxTacmapAlertProcessTimeMilliseconds, v => _maxProcessTime = TimeSpan.FromMilliseconds(v), true);
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
    private void OnRemove(Entity<TacMapMarineAlertComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private string GetAreaName(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var area, out var areaProto))
            return Loc.GetString("rmc-tacmap-alert-no-area");

        var properties = new List<string>();
        if (area.Value.Comp.CAS) properties.Add("CAS");
        if (area.Value.Comp.Fulton) properties.Add("Fulton");
        if (area.Value.Comp.Lasing) properties.Add("Lasing");
        if (area.Value.Comp.MortarPlacement) properties.Add("Mortar");
        if (area.Value.Comp.MortarFire) properties.Add("MortarFire");
        if (area.Value.Comp.Medevac) properties.Add("Medevac");
        if (area.Value.Comp.OB) properties.Add("OB");
        if (area.Value.Comp.SupplyDrop) properties.Add("Supply");
        if (area.Value.Comp.AvoidBioscan) properties.Add("NoBioscan");
        if (area.Value.Comp.NoTunnel) properties.Add("NoTunnel");
        if (area.Value.Comp.Unweedable) properties.Add("Unweedable");
        if (area.Value.Comp.BuildSpecial) properties.Add("BuildSpecial");
        if (!area.Value.Comp.ResinAllowed) properties.Add("NoResin");
        if (!area.Value.Comp.ResinConstructionAllowed) properties.Add("NoResinBuild");
        if (!area.Value.Comp.WeatherEnabled) properties.Add("NoWeather");
        if (area.Value.Comp.LandingZone) properties.Add("LZ");
        if (area.Value.Comp.WeedKilling) properties.Add("WeedKilling");
        if (area.Value.Comp.RetrieveItemObjective) properties.Add("RetrieveItem");

        var propertiesStr = properties.Count > 0 ? $" ({string.Join(", ", properties)})" : "";
        return $"{areaProto.Name}{propertiesStr}";
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (_marineAlertQueue.Count > 0)
        {
            while (_marineAlertQueue.TryDequeue(out var ent))
            {
                if (_timing.CurTime >= time + _maxProcessTime)
                    return;

                if (TerminatingOrDeleted(ent))
                    continue;

                _alerts.ShowAlert(ent, ent.Comp.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(ent))));
            }
        }

        var tacMapQuery = EntityQueryEnumerator<TacMapMarineAlertComponent>();
        while (tacMapQuery.MoveNext(out var uid, out var alert))
        {
            if (time < alert.NextUpdateTime)
                continue;

            _marineAlertQueue.Enqueue((uid, alert));
            alert.NextUpdateTime = time + alert.UpdateInterval;
        }
    }
}
