using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCBatSystem
{
    private bool TryWakeFromDisturbance(Entity<RMCBatHangingComponent> ent)
    {
        if (ent.Comp.DisturbanceRange <= 0)
            return false;

        var coords = Transform.GetMapCoordinates(ent.Owner);
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.DisturbanceRange))
        {
            if (mob.Owner == ent.Owner ||
                HasComp<RMCBatHangingComponent>(mob.Owner) ||
                !MobState.IsAlive(mob.Owner, mob.Comp))
            {
                continue;
            }

            if (!Random.Prob(ent.Comp.DisturbanceWakeChance))
                return false;

            Popup.PopupEntity(Loc.GetString("rmc-bat-wakes-disturbed", ("bat", ent.Owner)), ent.Owner);
            return true;
        }

        return false;
    }

    private bool CanHang(EntityUid uid, RMCBatHangingComponent comp)
    {
        if (!comp.RequireBlockedNorth)
            return true;

        var coords = Transform(uid).Coordinates.Offset(Direction.North.ToVec());
        return _turf.TryGetTileRef(coords, out var tile) &&
               _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable);
    }

    private void HangBat(Entity<RMCBatHangingComponent> ent)
    {
        ent.Comp.Hanging = true;
        StabilizeBat(ent.Owner);
        AlignBatToWall(ent);
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, true);
        RMCNpc.SleepNPC(ent.Owner);
    }

    private void WakeBat(Entity<RMCBatHangingComponent> ent)
    {
        if (!ent.Comp.Hanging)
            return;

        ent.Comp.Hanging = false;
        StabilizeBat(ent.Owner);
        Transform.SetLocalRotation(ent.Owner, Angle.Zero);
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, false);
        RMCNpc.WakeNPC(ent.Owner);
    }

    private void AlignBatToWall(Entity<RMCBatHangingComponent> ent)
    {
        var xform = Transform(ent.Owner);
        var coords = xform.Coordinates.SnapToGrid(EntityManager).Offset(ent.Comp.HangOffset);
        Transform.SetCoordinates(ent.Owner, xform, coords, Angle.Zero);
        Transform.SetLocalRotation(ent.Owner, Angle.Zero, xform);
    }

    private void StabilizeBat(EntityUid uid)
    {
        StopMovement(uid);

        if (!PhysicsQuery.TryComp(uid, out var physics))
            return;

        Physics.SetFixedRotation(uid, true, body: physics);
        Physics.SetAngularVelocity(uid, 0f, body: physics);
    }
}
