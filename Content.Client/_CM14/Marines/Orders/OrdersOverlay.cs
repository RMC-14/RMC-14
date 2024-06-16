using System.Numerics;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Marines.Orders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._CM14.Marines.Orders;

// This is all just copy pasted from the XenoPheromonesOverlay because I could not figure out how to generalize it easily
// TODO CM14: Just generalize this along with the xeno system. Possibly to be reused by other stuff as well?
public sealed class OrdersOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public OrdersOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.HasComponent<MarineComponent>(_players.LocalEntity))
            return;

        var handle = args.WorldHandle;
        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

        handle.UseShader(_shader);

        var moveOrders = _entity.AllEntityQueryEnumerator<MoveOrderComponent, SpriteComponent, TransformComponent>();
        while (moveOrders.MoveNext(out var uid, out var move, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, move.Icon, scaleMatrix, rotationMatrix);
        }

        var holdOrders = _entity.AllEntityQueryEnumerator<HoldOrderComponent, SpriteComponent, TransformComponent>();
        while (holdOrders.MoveNext(out var uid, out var hold, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, hold.Icon, scaleMatrix, rotationMatrix);
        }

        var focusOrders = _entity.AllEntityQueryEnumerator<FocusOrderComponent, SpriteComponent, TransformComponent>();
        while (focusOrders.MoveNext(out var uid, out var focus, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, focus.Icon, scaleMatrix, rotationMatrix);
        }
        handle.UseShader(null);
    }

    private void DrawIcon(
        Entity<SpriteComponent, TransformComponent> ent,
        in OverlayDrawArgs args,
        SpriteSpecifier icon,
        Matrix3x2 scaleMatrix,
        Matrix3x2 rotationMatrix)
    {
        var (_, sprite, xform) = ent;
        if (xform.MapID != args.MapId)
            return;

        var bounds = sprite.Bounds;

        var worldPos = _transform.GetWorldPosition(xform);

        if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
            return;

        var handle = args.WorldHandle;
        var worldMatrix = Matrix3x2.CreateTranslation(worldPos);
        var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
        var matrix = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
        handle.SetTransform(matrix);

        var texture = _sprite.GetFrame(icon, _timing.CurTime);

        var yOffset = (bounds.Height + sprite.Offset.Y) / 2f - (float) texture.Height / EyeManager.PixelsPerMeter * bounds.Height;
        var xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float) texture.Width / EyeManager.PixelsPerMeter - 0.25f;

        var position = new Vector2(xOffset, yOffset);
        handle.DrawTexture(texture, position);
    }
}
