using System.Numerics;
using Content.Client.Examine;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ContainerSystem _container;
    private readonly ExamineSystem _examine;
    private readonly TransformSystem _transform;
    private readonly EntityQuery<XenoComponent> _xenoQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;
    private readonly List<NightVisionRenderEntry> _entries = new();

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _examine = _entity.System<ExamineSystem>();
        _transform = _entity.System<TransformSystem>();
        _xenoQuery = _entity.GetEntityQuery<XenoComponent>();

        _shader = _prototype.Index<ShaderPrototype>("RMCNightVision").Instance().Duplicate();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_players.LocalEntity, out NightVisionComponent? nightVision) ||
            nightVision.State == NightVisionState.Off)
        {
            return;
        }

        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;

        _entries.Clear();
        var entities = _entity.EntityQueryEnumerator<RMCNightVisionVisibleComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out var visible, out var sprite, out var xform))
        {
            _entries.Add(new NightVisionRenderEntry((uid, sprite, xform),
                eye?.Position.MapId,
                nightVision.SeeThroughContainers,
                visible.Priority,
                visible.Transparency));
        }

        _entries.Sort(SortPriority);

        foreach (var entry in _entries)
        {
            Render(entry.Ent,
                entry.Map,
                handle,
                eyeRot,
                entry.NightVisionSeeThroughContainers,
                entry.Transparency);
        }

        if (_players.LocalEntity is { } player)
        {
            var inViewQuery = _entity.EntityQueryEnumerator<RMCNightVisionVisibleInViewComponent, SpriteComponent, TransformComponent>();
            while (inViewQuery.MoveNext(out var uid, out _, out var sprite, out var xform))
            {
                if (!_examine.InRangeUnOccluded(uid, player))
                    continue;

                Render((uid, sprite, xform),
                    eye?.Position.MapId,
                    handle,
                    eyeRot,
                    false,
                    null);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);

        if (!nightVision.Green)
            return;

        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }

    private static int SortPriority(NightVisionRenderEntry x, NightVisionRenderEntry y)
    {
        return x.Priority.CompareTo(y.Priority);
    }

    private void Render(Entity<SpriteComponent, TransformComponent> ent,
        MapId? map,
        DrawingHandleWorld handle,
        Angle eyeRot,
        bool seeThroughContainers,
        float? transparency)
    {
        var (uid, sprite, xform) = ent;
        if (xform.MapID != map)
            return;

        var seeThrough = seeThroughContainers && !_xenoQuery.HasComp(uid);
        if (!seeThrough && _container.IsEntityOrParentInContainer(uid, xform: xform))
            return;

        var (position, rotation) = _transform.GetWorldPositionRotation(xform);

        var colorCache = sprite.Color;
        if (transparency != null)
        {
            var color = sprite.Color * Color.White.WithAlpha(transparency.Value);
            sprite.Color = color;
        }
        sprite.Render(handle, eyeRot, rotation, position: position);
        if (transparency != null)
        {
            sprite.Color = colorCache;
        }
    }
}

public record struct NightVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    bool NightVisionSeeThroughContainers,
    int Priority,
    float? Transparency
);
