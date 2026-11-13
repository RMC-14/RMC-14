using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Mobs.Animals;

public abstract partial class RMCAnimalSystem
{
    protected void StopMovement(EntityUid uid)
    {
        if (PhysicsQuery.TryComp(uid, out var physics))
            Physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }

    protected bool TryMoveTowards(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMapCoordinates(uid);
        var targetMap = Transform.ToMapCoordinates(target, false);
        if (origin.MapId != targetMap.MapId)
            return false;

        var direction = targetMap.Position - origin.Position;
        if (direction.LengthSquared() < 0.01f)
            return false;

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveAwayFrom(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMapCoordinates(uid);
        var targetMap = Transform.ToMapCoordinates(target, false);
        if (origin.MapId != targetMap.MapId)
            return false;

        var direction = origin.Position - targetMap.Position;
        if (direction.LengthSquared() < 0.01f)
            direction = Random.NextAngle().RotateVec(Vector2.UnitX);

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveRandomly(EntityUid uid, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var direction = Random.NextAngle().RotateVec(Vector2.UnitX);
        Physics.SetLinearVelocity(uid, direction * speed, body: physics);
        return true;
    }
}
