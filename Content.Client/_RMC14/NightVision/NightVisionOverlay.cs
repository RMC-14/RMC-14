using System.Numerics;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly ContainerSystem _container;
    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<NightVisionRenderEntry> _entries = new();

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
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
                eyeRot,
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
                entry.EyeRot,
                entry.NightVisionSeeThroughContainers,
                entry.Transparency);
        }

        handle.SetTransform(Matrix3x2.Identity);
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

        var seeThrough = seeThroughContainers && !_entity.HasComponent<XenoComponent>(uid);
        if (!seeThrough && _container.IsEntityOrParentInContainer(uid))
            return;

        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

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
    Angle EyeRot,
    bool NightVisionSeeThroughContainers,
    int Priority,
    float? Transparency);
