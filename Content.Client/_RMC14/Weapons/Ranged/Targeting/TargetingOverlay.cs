using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Targeting;
using Content.Shared.Coordinates;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Weapons.Ranged.Targeting;

public sealed class TargetingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager _entManager;
    private readonly SpriteSystem _sprite;
    private readonly IGameTiming _timing;
    private readonly SharedTransformSystem _transform;

    // Animation timer
    private float _animTime;

    public TargetingOverlay(IEntityManager entManager, IGameTiming timing)
    {
        _entManager = entManager;
        _sprite = entManager.System<SpriteSystem>();
        _timing = timing;
        _transform = entManager.System<SharedTransformSystem>();
    }

    /// <summary>
    ///     Draws a line between any targeted entities and the entities targeting them.
    ///     Also draws a targeting visual on the targeted entity.
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<RMCTargetedComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var targetingLaserQuery = _entManager.GetEntityQuery<TargetingLaserComponent>();
        var worldHandle = args.WorldHandle;

        _animTime += (float)_timing.FrameTime.TotalSeconds;

        while (query.MoveNext(out var uid, out var targeted))
        {
            // Laser visuals
            foreach (var targeter in targeted.TargetedBy)
            {
                if (!targetingLaserQuery.TryGetComponent(targeter, out var targetingLaser) || !targetingLaser.ShowLaser)
                    continue;

                if (!xformQuery.TryGetComponent(targeter, out var gunXform) ||
                    !xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                if (xform.MapID != gunXform.MapID)
                    continue;

                var worldPos = _transform.GetWorldPosition(xform, xformQuery);
                var gunWorldPos = _transform.GetWorldPosition(gunXform, xformQuery);
                var diff = worldPos - gunWorldPos;
                var angle = diff.ToWorldAngle();
                var length = diff.Length() / 2f;
                var midPoint = gunWorldPos + diff / 2;
                const float width = 0.02f;

                var box = new Box2(-width, -length, width, length);
                var rotated = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

                var color = targetingLaser.CurrentLaserColor;
                var alpha = targetingLaser.GradualAlpha && targeted.AlphaMultipliers.TryGetValue(targeter, out var mult)
                    ? targetingLaser.LaserAlpha * mult
                    : targetingLaser.LaserAlpha;

                worldHandle.DrawRect(rotated, color.WithAlpha(alpha));
            }

            if (!xformQuery.TryGetComponent(uid, out var targetXform))
                continue;

            var worldPosCross = _transform.GetWorldPosition(targetXform, xformQuery);

            if (targeted.TargetType == TargetedEffects.None)
                continue;

            // Targeted crosshair visuals
            var lockOnState = targeted.SpotterState;
            string? directionState = null;

            switch (targeted.TargetType)
            {
                case TargetedEffects.Targeted:
                    lockOnState = targeted.LockOnState;
                    directionState = targeted.LockOnStateDirection;
                    break;
                case TargetedEffects.TargetedIntense:
                    lockOnState = targeted.LockOnStateIntense;
                    directionState = targeted.LockOnStateIntenseDirection;
                    break;
            }

            var lockOnRsi = _sprite.GetState(new SpriteSpecifier.Rsi(targeted.RsiPath, lockOnState));
            var time = _animTime % lockOnRsi.AnimationLength;
            var delay = 0f;
            var frameIndex = 0;
            for (var i = 0; i < lockOnRsi.DelayCount; i++)
            {
                delay += lockOnRsi.GetDelay(i);
                if (!(time < delay))
                    continue;

                frameIndex = i;
                break;
            }

            var lockonTexture = lockOnRsi.GetFrames(RsiDirection.South)[frameIndex];
            var centerOffset = new Vector2(lockonTexture.Width / 2f / EyeManager.PixelsPerMeter, lockonTexture.Height / 2f / EyeManager.PixelsPerMeter);
            worldHandle.DrawTexture(lockonTexture, worldPosCross - centerOffset);

            if (directionState == null || !targeted.ShowDirection)
                continue;

            // Direction arrow visual
            var targetingOrigin = targeted.TargetedBy.Last().ToCoordinates();
            var targetLocation = uid.ToCoordinates();
            var direction = Angle.FromDegrees(90).RotateVec(_transform.ToMapCoordinates(targetingOrigin).Position - _transform.ToMapCoordinates(targetLocation).Position).ToAngle().GetCardinalDir();

            var rsiDirection = direction switch
            {
                Direction.East => RsiDirection.East,
                Direction.North => RsiDirection.North,
                Direction.West => RsiDirection.West,
                _ => RsiDirection.South,
            };

            var directionRsi = _sprite.GetState(new SpriteSpecifier.Rsi(targeted.RsiPath, directionState));
            var directionTexture = directionRsi.GetFrame(rsiDirection, 0);
            worldHandle.DrawTexture(directionTexture, worldPosCross - centerOffset);
        }
    }
}
