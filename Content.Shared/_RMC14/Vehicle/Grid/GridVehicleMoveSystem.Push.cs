using System;
using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Vehicle;

public sealed partial class GridVehicleMoverSystem : EntitySystem
{
    private Vector2i GetInputDirection(InputMoverComponent input)
    {
        var buttons = input.HeldMoveButtons;
        var dir = Vector2i.Zero;

        if ((buttons & MoveButtons.Up) != 0) dir += new Vector2i(0, 1);
        if ((buttons & MoveButtons.Down) != 0) dir += new Vector2i(0, -1);
        if ((buttons & MoveButtons.Right) != 0) dir += new Vector2i(1, 0);
        if ((buttons & MoveButtons.Left) != 0) dir += new Vector2i(-1, 0);

        if (dir == Vector2i.Zero)
            return dir;

        if (dir.X != 0 && dir.Y != 0)
        {
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
                dir = new Vector2i(Math.Sign(dir.X), 0);
            else
                dir = new Vector2i(0, Math.Sign(dir.Y));
        }

        return dir;
    }

    private Vector2i GetMoverInput(EntityUid uid, GridVehicleMoverComponent mover, VehicleComponent vehicle, out bool pushing)
    {
        pushing = false;
        if (vehicle.Operator is { } op && TryComp<InputMoverComponent>(op, out var inputComp))
            return GetInputDirection(inputComp);

        if (vehicle.Operator != null)
            return Vector2i.Zero;

        if (!TryGetActivePusher(uid, mover, out var pusher))
            return Vector2i.Zero;

        pushing = true;
        if (!CanPushNow(mover))
            return Vector2i.Zero;

        var pushDir = GetPushDirection(uid, pusher);
        if (pushDir == Vector2i.Zero)
            return Vector2i.Zero;

        pushing = true;
        return pushDir;
    }

    private bool TryGetActivePusher(EntityUid uid, GridVehicleMoverComponent mover, out EntityUid pusher)
    {
        pusher = default;
        if (!physicsQ.TryComp(uid, out var body) || !body.CanCollide)
            return false;

        if (!fixtureQ.TryComp(uid, out var fixtures))
            return false;

        var vehiclePos = transform.GetWorldPosition(uid);
        var contacts = physics.GetContacts((uid, fixtures));
        var bestScore = 0f;

        while (contacts.MoveNext(out var contact))
        {
            if (contact == null || !contact.IsTouching || !contact.Hard)
                continue;

            var other = contact.OtherEnt(uid);
            if (!HasComp<XenoComponent>(other))
                continue;

            if (!CanXenoMoveVehicle(mover, other))
                continue;

            if (!TryComp<InputMoverComponent>(other, out var input))
                continue;

            var dir = GetInputDirection(input);
            if (dir == Vector2i.Zero)
                continue;

            var otherPos = transform.GetWorldPosition(other);
            var toVehicle = vehiclePos - otherPos;
            if (toVehicle.LengthSquared() <= 0.0001f)
                continue;

            var inputVec = new Vector2(dir.X, dir.Y);
            var score = Vector2.Dot(inputVec, Vector2.Normalize(toVehicle));
            if (score <= 0f)
                continue;

            if (score > bestScore)
            {
                bestScore = score;
                pusher = other;
            }
        }

        return bestScore > 0f;
    }

    private Vector2i GetPushDirection(EntityUid uid, EntityUid pusher)
    {
        var vehiclePos = transform.GetWorldPosition(uid);
        var pusherPos = transform.GetWorldPosition(pusher);
        var delta = vehiclePos - pusherPos;
        if (delta.LengthSquared() <= 0.0001f)
            return Vector2i.Zero;

        return Angle.FromWorldVec(delta).GetCardinalDir().ToIntVec();
    }

    private bool CanPushNow(GridVehicleMoverComponent mover)
    {
        if (mover.PushCooldown <= 0f)
            return true;

        return _timing.CurTime >= mover.NextPushTime;
    }

    private bool CanXenoMoveVehicle(GridVehicleMoverComponent mover, EntityUid xeno)
    {
        if (mover.XenoMoveMinimumSize is not { } minSize)
            return true;

        if (!_size.TryGetSize(xeno, out var size))
            return false;

        return size >= minSize;
    }
}
