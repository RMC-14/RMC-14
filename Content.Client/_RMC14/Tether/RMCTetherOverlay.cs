using Content.Shared._RMC14.Tether;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Tether;

public sealed class RMCTetherOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _playerManager;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;


    public RMCTetherOverlay(IEntityManager entManager, IPlayerManager playerManager)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _sprite = entManager.System<SpriteSystem>();
        _transform = entManager.System<SharedTransformSystem>();
    }

    /// <summary>
    ///     Draws a texture between an entity and it's origin.
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<RMCTetherComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var worldHandle = args.WorldHandle;

        while (query.MoveNext(out var uid, out var tether))
        {
            var origin = tether.TetherOrigin;

            if (origin == null)
                continue;

            if (!xformQuery.TryGetComponent(origin, out var shooterXform) ||
                !xformQuery.TryGetComponent(uid, out var xform))
                continue;

            if (xform.MapID != shooterXform.MapID)
                continue;

            if (_playerManager.LocalEntity is not { } player || player == origin && !tether.VisibleToOrigin)
                continue;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);
            var gunWorldPos = _transform.GetWorldPosition(origin.Value, xformQuery);
            var diff = worldPos - gunWorldPos;
            var angle = diff.ToWorldAngle();
            var length = diff.Length() / 2f;
            var midPoint = gunWorldPos + diff / 2;
            var width = tether.TetherWidth;

            var box = new Box2(-width, -length, width, length);
            var rotated = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

            var laserTexture = _sprite.GetState(new SpriteSpecifier.Rsi(tether.RsiPath, tether.TetherState));
            var laserRsi = laserTexture.Frame0;

            worldHandle.DrawTextureRect(laserRsi, rotated);
        }
    }
}
