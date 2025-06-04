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
        var (areaName, ceilingLevel, restrictions) = GetAreaInfo(ent);
        _alerts.ShowAlert(ent, ent.Comp.Alert,
            severity: ceilingLevel,
            dynamicMessage: Loc.GetString("rmc-tacmap-alert-area-info",
                ("area", areaName),
                ("ceilingLevel", ceilingLevel),
                ("restrictions", restrictions)));
    }

    private void OnRemove(Entity<TacMapMarineAlertComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private (string areaName, short ceilingLevel, string restrictions) GetAreaInfo(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var area, out var areaProto))
            return (Loc.GetString("rmc-tacmap-alert-no-area"), 0, string.Empty);

        var restrictions = new List<string>();

        // Determine ceiling level based on restrictions
        short ceilingLevel = 0;
        if (area.Value.Comp.OB)
            ceilingLevel = 4;
        else if (area.Value.Comp.CAS)
            ceilingLevel = 3;
        else if (area.Value.Comp.SupplyDrop || area.Value.Comp.MortarFire)
            ceilingLevel = 2;
        else if (area.Value.Comp.MortarPlacement || area.Value.Comp.Lasing || area.Value.Comp.Medevac)
            ceilingLevel = 1;

        // Build the restrictions string
        var allowedActions = new List<string>();
        var restrictedActions = new List<string>();

        // Add ceiling level restrictions
        restrictedActions.Add(Loc.GetString($"rmc-tacmap-alert-ceiling-level-{ceilingLevel}"));

        // Add allowed actions based on ceiling level
        if (ceilingLevel < 4)
            allowedActions.Add("OB");
        if (ceilingLevel < 3)
            allowedActions.Add("CAS");
        if (ceilingLevel < 2)
        {
            allowedActions.Add("Supply Drops");
            allowedActions.Add("Mortar Fire");
        }
        if (ceilingLevel < 1)
        {
            allowedActions.Add("Mortar Placement");
            allowedActions.Add("Lasing");
            allowedActions.Add("Medevac");
        }

        // Add tunnel and resin restrictions
        if (area.Value.Comp.NoTunnel)
            restrictedActions.Add("Tunneling");
        if (area.Value.Comp.Unweedable)
            restrictedActions.Add("Weed Placement");
        else if (!area.Value.Comp.ResinAllowed)
            restrictedActions.Add("Resin Placement");

        var restrictionsStr = "\n";
        if (allowedActions.Count > 0)
            restrictionsStr += Loc.GetString("rmc-tacmap-alert-allowed-actions", ("actions", string.Join(", ", allowedActions))) + "\n";
        if (restrictedActions.Count > 0)
            restrictionsStr += string.Join("\n", restrictedActions);

        return (areaProto.Name, ceilingLevel, restrictionsStr);
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

                var (areaName, ceilingLevel, restrictions) = GetAreaInfo(ent);
                _alerts.ShowAlert(ent, ent.Comp.Alert,
                    severity: ceilingLevel,
                    dynamicMessage: Loc.GetString("rmc-tacmap-alert-area-info",
                        ("area", areaName),
                        ("ceilingLevel", ceilingLevel),
                        ("restrictions", restrictions)));
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
