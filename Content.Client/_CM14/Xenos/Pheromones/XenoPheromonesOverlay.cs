using System.Numerics;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Pheromones;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._CM14.Xenos.Pheromones;

public sealed class XenoPheromonesOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private readonly ShaderInstance _shader;

    private EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public XenoPheromonesOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.HasComponent<XenoComponent>(_players.LocalEntity))
            return;

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var scaleMatrix = Matrix3.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3.CreateRotation(-eyeRot);

        handle.UseShader(_shader);

        var recoveryPheromones = _entity.AllEntityQueryEnumerator<XenoRecoveryPheromonesComponent, SpriteComponent, TransformComponent>();
        while (recoveryPheromones.MoveNext(out var uid, out var recovery, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, recovery.Icon, scaleMatrix, rotationMatrix);
        }

        var wardingPheromones = _entity.AllEntityQueryEnumerator<XenoWardingPheromonesComponent, SpriteComponent, TransformComponent>();
        while (wardingPheromones.MoveNext(out var uid, out var warding, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, warding.Icon, scaleMatrix, rotationMatrix);
        }

        var frenzyPheromones = _entity.AllEntityQueryEnumerator<XenoFrenzyPheromonesComponent, SpriteComponent, TransformComponent>();
        while (frenzyPheromones.MoveNext(out var uid, out var frenzy, out var sprite, out var xform))
        {
            DrawIcon((uid, sprite, xform), in args, frenzy.Icon, scaleMatrix, rotationMatrix);
        }

        var sources = _entity.AllEntityQueryEnumerator<ActiveXenoPheromonesComponent, SpriteComponent, TransformComponent>();
        while (sources.MoveNext(out var uid, out var pheromones, out var sprite, out var xform))
        {
            var emitting = pheromones.Pheromones;
            var name = $"aura_{emitting.ToString().ToLowerInvariant()}";
            var icon = new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_pheromones_hud.rsi"), name);

            DrawIcon((uid, sprite, xform), in args, icon, scaleMatrix, rotationMatrix);
        }

        handle.UseShader(null);
    }

    private void DrawIcon(
        Entity<SpriteComponent, TransformComponent> ent,
        in OverlayDrawArgs args,
        SpriteSpecifier icon,
        Matrix3 scaleMatrix,
        Matrix3 rotationMatrix)
    {
        var (_, sprite, xform) = ent;
        if (xform.MapID != args.MapId)
            return;

        var bounds = sprite.Bounds;

        var worldPos = _transform.GetWorldPosition(xform, _xformQuery);

        if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
            return;

        var handle = args.WorldHandle;
        var worldMatrix = Matrix3.CreateTranslation(worldPos);
        Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
        Matrix3.Multiply(rotationMatrix, scaledWorld, out var matrix);
        handle.SetTransform(matrix);

        var texture = _sprite.GetFrame(icon, _timing.CurTime);

        var yOffset = (bounds.Height + sprite.Offset.Y) / 2f - (float) texture.Height / EyeManager.PixelsPerMeter * bounds.Height;
        var xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float) texture.Width / EyeManager.PixelsPerMeter - 0.25f;

        var position = new Vector2(xOffset, yOffset);
        handle.DrawTexture(texture, position);
    }
}
