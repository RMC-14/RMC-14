using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rotate;

public sealed class XenoRotateSystem : EntitySystem
{
    [Dependency] private readonly RotateToFaceSystem _rotateTo = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public void RotateXeno(EntityUid uid, Direction direction, TimeSpan? delay = null)
    {
        var rotationComp = EnsureComp<XenoRotateComponent>(uid);
        rotationComp.TargetDirection = direction;
        rotationComp.Delay = delay ?? rotationComp.Delay;
        Dirty(uid, rotationComp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoRotateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rotate, out var xform))
        {
            if (rotate.NextRotation > _timing.CurTime)
                continue;

            if (rotate.FirstRotation)
            {
                rotate.OriginalDirection = _transform.GetWorldRotation(xform).GetDir();
                rotate.NextRotation = _timing.CurTime + rotate.Delay;
                rotate.FirstRotation = false;
                Dirty(uid, rotate);

                _rotateTo.TryFaceAngle(uid, rotate.TargetDirection.ToAngle(), xform);
            }
            else if (rotate.OriginalDirection != null)
            {
                _rotateTo.TryFaceAngle(uid, rotate.OriginalDirection.Value.ToAngle(), xform);
                RemCompDeferred<XenoRotateComponent>(uid);
            }
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoRotateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rotate, out var xform))
        {
            _rotateTo.TryFaceAngle(uid, rotate.TargetDirection.ToAngle(), xform);
        }
    }
}
