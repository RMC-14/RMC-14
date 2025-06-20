using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IConfigurationManager _config = default!;    private readonly Queue<Entity<AreaInfoComponent>> _marineAlertCopyQueue = new();

    private TimeSpan _maxProcessTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrantAreaInfoComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantAreaInfoComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<AreaInfoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AreaInfoComponent, ComponentRemove>(OnRemove);

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
            return;        if ((ent.Comp.Slots & args.SlotFlags) == 0)
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

    private (string areaName, short ceilingLevel, string restrictions) GetAreaInfo(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var area, out var areaProto))
            return (Loc.GetString("rmc-tacmap-alert-no-area"), 0, string.Empty);

        // Determine ceiling level based on area restrictions
        // Higher ceiling level = more protection = more things blocked
        short ceilingLevel = 0; // CEILING_NO_PROTECTION - everything allowed

        // Check for highest protection level first
        // We check what's BLOCKED (false values) to determine ceiling level
        if (!area.Value.Comp.OB) // OB is blocked
            ceilingLevel = 4; // CEILING_PROTECTION_TIER_4
        else if (!area.Value.Comp.CAS) // CAS is blocked
            ceilingLevel = 3; // CEILING_PROTECTION_TIER_3
        else if (!area.Value.Comp.SupplyDrop || !area.Value.Comp.MortarFire) // Supply drops or mortar fire blocked
            ceilingLevel = 2; // CEILING_PROTECTION_TIER_2
        else if (!area.Value.Comp.MortarPlacement || !area.Value.Comp.Lasing || !area.Value.Comp.Medevac) // Mortar placement, lasing, or medevac blocked
            ceilingLevel = 1; // CEILING_PROTECTION_TIER_1

        // Build the restrictions string with clean formatting
        var allowedActions = new List<string>();
        var restrictedActions = new List<string>();

        // Check each action and categorize
        if (area.Value.Comp.OB)
            allowedActions.Add("Orbital Strike");
        else
            restrictedActions.Add("Orbital Strike");

        if (area.Value.Comp.CAS)
            allowedActions.Add("Close Air Support");
        else
            restrictedActions.Add("Close Air Support");

        if (area.Value.Comp.SupplyDrop)
            allowedActions.Add("Supply Drops");
        else
            restrictedActions.Add("Supply Drops");

        if (area.Value.Comp.MortarFire)
            allowedActions.Add("Mortar Fire");
        else
            restrictedActions.Add("Mortar Fire");

        if (area.Value.Comp.MortarPlacement)
            allowedActions.Add("Mortar Placement");
        else
            restrictedActions.Add("Mortar Placement");

        if (area.Value.Comp.Lasing)
            allowedActions.Add("Laser Designation");
        else
            restrictedActions.Add("Laser Designation");

        if (area.Value.Comp.Medevac)
            allowedActions.Add("Casualty Evacuation");
        else
            restrictedActions.Add("Casualty Evacuation");

        // Add special restrictions
        if (area.Value.Comp.NoTunnel)
            restrictedActions.Add("Tunneling");
        if (area.Value.Comp.Unweedable)
            restrictedActions.Add("Weed Placement");
        else if (!area.Value.Comp.ResinAllowed)
            restrictedActions.Add("Resin Structures");        // Build final restrictions string with clean formatting
        var restrictionsStr = $"\nCeiling level: {ceilingLevel}";

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

        return (areaProto.Name, ceilingLevel, restrictionsStr);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (_marineAlertCopyQueue.Count > 0)
        {            while (_marineAlertCopyQueue.TryDequeue(out var ent))
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
