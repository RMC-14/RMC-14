using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Explosion;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleSystem
{
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private void OnDemolitionInteractUsing(Entity<VehicleDemolitionComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.Used))
            return;

        if (!_skills.HasSkills(args.User, ent.Comp.Skills))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-demolition-no-skill"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!IsVehicleFrameDestroyed(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-demolition-frame-intact"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        if (ent.Comp.Rigging)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-demolition-busy"), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfter, new VehicleDemolitionDoAfterEvent(), ent.Owner, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        ent.Comp.Rigging = true;
        _audio.PlayPvs(ent.Comp.PlantSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("rmc-vehicle-demolition-start"), args.User, args.User);
    }

    private void OnDemolitionDoAfter(Entity<VehicleDemolitionComponent> ent, ref VehicleDemolitionDoAfterEvent args)
    {
        ent.Comp.Rigging = false;

        if (args.Cancelled || args.Handled || _net.IsClient)
            return;

        if (!IsVehicleFrameDestroyed(ent.Owner))
            return;

        args.Handled = true;

        if (ent.Comp.DeleteUsedItem && args.Used is { } used && !TerminatingOrDeleted(used))
            QueueDel(used);

        var now = _timing.CurTime;
        ent.Comp.Armed = true;
        ent.Comp.DetonateAt = now + TimeSpan.FromSeconds(ent.Comp.TimerDelay);
        ent.Comp.NextBeepAt = now;

        _popup.PopupEntity(Loc.GetString("rmc-vehicle-demolition-armed"), args.User, args.User, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VehicleDemolitionComponent>();
        while (query.MoveNext(out var uid, out var demolition))
        {
            if (!demolition.Armed)
                continue;

            if (now >= demolition.NextBeepAt)
            {
                _audio.PlayPvs(demolition.BeepSound, uid);
                demolition.NextBeepAt = now + TimeSpan.FromSeconds(demolition.BeepInterval);
            }

            if (now >= demolition.DetonateAt)
            {
                demolition.Armed = false;
                DetonateVehicle((uid, demolition));
            }
        }
    }

    private void DetonateVehicle(Entity<VehicleDemolitionComponent> ent)
    {
        var mapCoords = _transform.GetMapCoordinates(ent.Owner);

        EjectAllOccupants(ent.Owner, mapCoords);

        if (mapCoords.MapId != MapId.Nullspace)
        {
            var explosion = Spawn(ent.Comp.Explosion, mapCoords);
            _rmcExplosion.TriggerExplosive(explosion);
        }

        QueueDel(ent.Owner);
    }

    private void EjectAllOccupants(EntityUid vehicle, MapCoordinates destination)
    {
        if (destination.MapId == MapId.Nullspace)
            return;

        if (!TryComp(vehicle, out VehicleInteriorComponent? interior))
            return;

        var occupants = new List<EntityUid>(interior.Passengers.Count + interior.Xenos.Count);
        occupants.AddRange(interior.Passengers);
        occupants.AddRange(interior.Xenos);

        foreach (var occupant in occupants)
        {
            if (TerminatingOrDeleted(occupant))
                continue;

            _rmcTeleporter.HandlePulling(occupant, destination);
        }
    }

    public bool IsVehicleFrameDestroyed(EntityUid vehicle)
    {
        return TryComp(vehicle, out HardpointIntegrityComponent? integrity) && integrity.Integrity <= 0f;
    }
}
