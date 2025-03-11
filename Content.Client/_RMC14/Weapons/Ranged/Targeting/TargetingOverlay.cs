using Content.Shared._RMC14.Targeting;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Weapons.Ranged.Targeting;

public sealed class TargetingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private IEntityManager _entManager;

    public TargetingOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<TargetedComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var targetingLaserQuery = _entManager.GetEntityQuery<TargetingLaserComponent>();
        var targetingQuery = _entManager.GetEntityQuery<TargetingComponent>();
        var worldHandle = args.WorldHandle;
        var xformSystem = _entManager.System<SharedTransformSystem>();

        while (query.MoveNext(out var uid, out var targeted))
        {
            foreach (var gun in targeted.TargetedBy)
            {
                if (!targetingLaserQuery.TryGetComponent(gun, out var targetingLaser) || !targetingLaser.ShowLaser)
                    continue;

                if (!xformQuery.TryGetComponent(gun, out var gunXform) ||
                    !xformQuery.TryGetComponent(uid, out var xform))
                {
                    continue;
                }

                if (xform.MapID != gunXform.MapID)
                    continue;

                var worldPos = xformSystem.GetWorldPosition(xform, xformQuery);
                var gunWorldPos = xformSystem.GetWorldPosition(gunXform, xformQuery);
                var diff = worldPos - gunWorldPos;
                var angle = diff.ToWorldAngle();
                var length = diff.Length() / 2f;
                var midPoint = gunWorldPos + diff / 2;
                const float Width = 0.02f;

                var box = new Box2(-Width, -length, Width, length);
                var rotated = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

                var color = targetingLaser.LaserColor;
                var alpha = targetingLaser.LaserAlpha;

                if (targetingQuery.TryGetComponent(gun, out var targeting) && targetingLaser.GradualAlpha)
                    alpha *= targeting.AlphaMultiplier;

                worldHandle.DrawRect(rotated, color.WithAlpha(alpha));
            }
        }
    }
}
