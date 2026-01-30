using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class AreaInfoSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Queue<Entity<AreaInfoComponent>> _marineAlertCopyQueue = new();

    private TimeSpan _maxProcessTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantAreaInfoComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantAreaInfoComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<AreaInfoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AreaInfoComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<AreaInfoComponent, MoveEvent>(OnMoveEvent);

        Subs.CVar(_config, RMCCVars.RMCMaxTacmapAlertProcessTimeMilliseconds, v => _maxProcessTime = TimeSpan.FromMilliseconds(v), true);
    }

    private void OnGotEquipped(Entity<GrantAreaInfoComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        EnsureComp<AreaInfoComponent>(args.Equipee);
    }

    private void OnGotUnequipped(Entity<GrantAreaInfoComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;
        if (!_inv.TryGetInventoryEntity<GrantAreaInfoComponent>(args.Equipee, out _))
            RemCompDeferred<AreaInfoComponent>(args.Equipee);
    }
    private void OnMapInit(Entity<AreaInfoComponent> ent, ref MapInitEvent args)
    {
        var (areaName, ceilingLevel, restrictions) = GetAreaInfo(ent);
        _alerts.ShowAlert(ent, ent.Comp.Alert,
            severity: ceilingLevel,
            dynamicMessage: Loc.GetString("rmc-area-info",
                ("area", areaName),
                ("ceilingLevel", ceilingLevel),
                ("restrictions", restrictions)));
    }
    private void OnRemove(Entity<AreaInfoComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnMoveEvent(Entity<AreaInfoComponent> ent, ref MoveEvent args)
    {
        if (_timing.ApplyingState)
            return;
        // update the alert when they move to a new area
        var (areaName, ceilingLevel, restrictions) = GetAreaInfo(ent);
        _alerts.ShowAlert(ent, ent.Comp.Alert,
            severity: ceilingLevel,
            dynamicMessage: Loc.GetString("rmc-area-info",
                ("area", areaName),
                ("ceilingLevel", ceilingLevel),
                ("restrictions", restrictions)));
    }

    private (string areaName, short ceilingLevel, string restrictions) GetAreaInfo(EntityUid ent)
    {
        var coordinates = ent.ToCoordinates();
        if (!_area.TryGetArea(coordinates, out var area, out var areaProto))
            return (Loc.GetString("rmc-tacmap-alert-no-area"), 0, string.Empty);


        short ceilingLevel = 0;
        short severityToUse = 0;

        // Check for hive core protection first (blocks everything including OB, has range ~11.85)
        bool hasHiveCoreProtection = IsProtectedByRoofing(coordinates, r => !r.Comp.CanOrbitalBombard && r.Comp.Range > 10);        // Check for pylon protection (blocks CAS/Mortar but allows OB, has range ~8.46)
        bool hasPylonProtection = IsProtectedByRoofing(coordinates, r => r.Comp.CanOrbitalBombard && !r.Comp.CanCAS && r.Comp.Range < 10);

        // Determine ceiling level based on effective protection (including roofing entities)
        // Note: severityToUse is offset by +1 because roofnull is at index 0 (for "no area" case)
        if (!_area.CanOrbitalBombard(coordinates, out var roofed))
        {
            ceilingLevel = 4;
            severityToUse = hasHiveCoreProtection ? (short)7 : (short)5;
        }
        else if (!_area.CanCAS(coordinates))
        {
            ceilingLevel = 3;
            severityToUse = hasPylonProtection ? (short)6 : (short)4;
        }
        else if (!_area.CanSupplyDrop(_transform.ToMapCoordinates(coordinates)) || !_area.CanMortarFire(coordinates))
        {
            ceilingLevel = 2;
            severityToUse = (short)3;
        }
        else if (!_area.CanMortarPlacement(coordinates) || !_area.CanLase(coordinates) || !_area.CanMedevac(coordinates) || !_area.CanParadrop(coordinates))
        {
            ceilingLevel = 1;
            severityToUse = (short)2;
        }
        else
        {
            ceilingLevel = 0;
            severityToUse = (short)1;
        }

        // Build the restrictions string with clean formatting
        var allowedActions = new List<string>();
        var restrictedActions = new List<string>();

        if (_area.CanOrbitalBombard(coordinates, out _))
            allowedActions.Add("Orbital Strike");
        else
            restrictedActions.Add("Orbital Strike");

        if (_area.CanCAS(coordinates))
            allowedActions.Add("Close Air Support");
        else
            restrictedActions.Add("Close Air Support");

        if (_area.CanSupplyDrop(coordinates.ToMap(_entityManager, _transform)))
            allowedActions.Add("Supply Drops");
        else
            restrictedActions.Add("Supply Drops");

        if (_area.CanMortarFire(coordinates))
            allowedActions.Add("Mortar Fire");
        else
            restrictedActions.Add("Mortar Fire");

        if (_area.CanMortarPlacement(coordinates))
            allowedActions.Add("Mortar Placement");
        else
            restrictedActions.Add("Mortar Placement");

        if (_area.CanLase(coordinates))
            allowedActions.Add("Laser Designation");
        else
            restrictedActions.Add("Laser Designation");

        if (area.Value.Comp.Medevac)
            allowedActions.Add("Casualty Evacuation");
        else
            restrictedActions.Add("Casualty Evacuation");

        if (area.Value.Comp.Paradropping)
            allowedActions.Add("Paradropping");
        else
            restrictedActions.Add("Paradropping");

        // Add special restrictions
        if (area.Value.Comp.NoTunnel)
            restrictedActions.Add("Tunneling");
        if (area.Value.Comp.Unweedable)
            restrictedActions.Add("Weed Placement");
        else if (!area.Value.Comp.ResinAllowed)
            restrictedActions.Add("Resin Structures");

        var protectionSource = "";
        if (hasHiveCoreProtection)
            protectionSource = "\nProtection: Hive Core";
        else if (hasPylonProtection)
            protectionSource = "\nProtection: Hive Pylon";

        var restrictionsStr = $"\nCeiling level: {ceilingLevel}{protectionSource}";

        if (allowedActions.Count > 0)
        {
            restrictionsStr += "\n\nAllowed:";
            restrictionsStr += "\n• " + string.Join("\n• ", allowedActions);
        }

        if (restrictedActions.Count > 0)
        {
            restrictionsStr += "\n\nBlocked:";
            restrictionsStr += "\n• " + string.Join("\n• ", restrictedActions);
        }

        return (areaProto.Name, severityToUse, restrictionsStr);
    }

    private bool IsProtectedByRoofing(EntityCoordinates coordinates, Predicate<Entity<RoofingEntityComponent>> predicate)
    {
        var roofs = EntityQueryEnumerator<RoofingEntityComponent>();
        while (roofs.MoveNext(out var uid, out var roof))
        {
            if (!predicate((uid, roof)))
                continue;

            if (coordinates.TryDistance(_entityManager, uid.ToCoordinates(), out var distance) &&
                distance <= roof.Range)
            {
                return true;
            }
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (_marineAlertCopyQueue.Count > 0)
        {
            while (_marineAlertCopyQueue.TryDequeue(out var ent))
            {
                if (_timing.CurTime >= time + _maxProcessTime)
                    return;

                if (TerminatingOrDeleted(ent))
                    continue;

                var (areaName, ceilingLevel, restrictions) = GetAreaInfo(ent);
                _alerts.ShowAlert(ent, ent.Comp.Alert,
                    severity: ceilingLevel,
                    dynamicMessage: Loc.GetString("rmc-area-info",
                        ("area", areaName),
                        ("ceilingLevel", ceilingLevel),
                        ("restrictions", restrictions)));
            }
        }

        var tacMapQuery = EntityQueryEnumerator<AreaInfoComponent>();
        while (tacMapQuery.MoveNext(out var uid, out var alert))
        {
            if (time < alert.NextUpdateTime)
                continue;
            _marineAlertCopyQueue.Enqueue((uid, alert));
            alert.NextUpdateTime = time + alert.UpdateInterval;
        }
    }
}
