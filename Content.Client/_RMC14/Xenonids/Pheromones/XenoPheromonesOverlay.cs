using System.Collections.Immutable;
using System.Numerics;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._RMC14.Xenonids.Pheromones;

public sealed class XenoPheromonesOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ImmutableArray<XenoPheromones> AllPheromones =
        Enum.GetValues<XenoPheromones>().ToImmutableArray();

    private readonly ContainerSystem _container;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private readonly EntityQuery<TransformComponent> _xformQuery;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public XenoPheromonesOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        _xformQuery = _entity.GetEntityQuery<TransformComponent>();

        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.HasComponent<XenoComponent>(_players.LocalEntity) && !_entity.HasComponent<CMGhostXenoHudComponent>(_players.LocalEntity))
            return;

        var handle = args.WorldHandle;
        var eyeRot = args.Viewport.Eye?.Rotation ?? default;
        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

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

        var sources = _entity.AllEntityQueryEnumerator<XenoActivePheromonesComponent, SpriteComponent, TransformComponent>();
        while (sources.MoveNext(out var uid, out var pheromones, out var sprite, out var xform))
        {
            var emitting = pheromones.Pheromones;
            var name = $"aura_{emitting.ToString().ToLowerInvariant()}";
            var icon = new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_pheromones_hud.rsi"), name);

            DrawIcon((uid, sprite, xform), in args, icon, scaleMatrix, rotationMatrix);
        }

        var leaders = _entity.AllEntityQueryEnumerator<HiveLeaderComponent, SpriteComponent, TransformComponent>();
        while (leaders.MoveNext(out var uid, out var leader, out var sprite, out var xform))
        {
            if (!_container.TryGetContainer(uid, leader.PheromonesContainerId, out var container) ||
                !container.ContainedEntities.TryFirstOrNull(out var first) ||
                !_entity.TryGetComponent(first, out XenoActivePheromonesComponent? active))
            {
                continue;
            }

            var emitting = active.Pheromones;
            var name = $"aura_{emitting.ToString().ToLowerInvariant()}";
            var icon = new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_pheromones_hud.rsi"), name);

            DrawIcon((uid, sprite, xform), in args, icon, scaleMatrix, rotationMatrix);
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

        var worldPos = _transform.GetWorldPosition(xform, _xformQuery);

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
