using Content.Shared._RMC14.Targeting;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Weapons.Ranged.Targeting;

public sealed class TargetingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IEntityManager _entManager;

    public TargetingOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    /// <summary>
    ///     Draws a line between any targeted entities and the entities targeting them.
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<RMCTargetedComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var targetingLaserQuery = _entManager.GetEntityQuery<TargetingLaserComponent>();
        var worldHandle = args.WorldHandle;
        var xformSystem = _entManager.System<SharedTransformSystem>();

        while (query.MoveNext(out var uid, out var targeted))
        {
            foreach (var targeter in targeted.TargetedBy)
            {
                if (!targetingLaserQuery.TryGetComponent(targeter, out var targetingLaser) || !targetingLaser.ShowLaser)
                    continue;

                if (!xformQuery.TryGetComponent(targeter, out var gunXform) ||
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
                const float width = 0.02f;

                var box = new Box2(-width, -length, width, length);
                var rotated = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

                var color = targetingLaser.CurrentLaserColor;
                var alpha = 0f;

                if (targetingLaser.GradualAlpha)
                {
                    if(targeted.AlphaMultipliers.TryGetValue(targeter, out var multiplier))
                        alpha = targetingLaser.LaserAlpha * multiplier;
                }
                else
                    alpha = targetingLaser.LaserAlpha;

                worldHandle.DrawRect(rotated, color.WithAlpha(alpha));
            }
        }
    }
}
