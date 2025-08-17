using Content.Client.SubFloor;
using Content.Shared._RMC14.Vents;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;
using static Content.Shared.DrawDepth.DrawDepth;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._RMC14.VentCrawl;
public sealed class VentCrawlIconOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly ContainerSystem _container;

    private readonly EntityQuery<TransformComponent> _xformQuery;

    private readonly ResPath _rsiPath = new("/Textures/_RMC14/Interface/vent_crawl.rsi");

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public VentCrawlIconOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        ZIndex = (int)HighFloorObjects;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.HasComponent<VentSightComponent>(_players.LocalEntity))
            return;

        var handle = args.WorldHandle;
        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

        var ventCrawlers = _entity.AllEntityQueryEnumerator<VentCrawlingComponent, VentCrawlerComponent, TransformComponent, SpriteComponent>();

        while (ventCrawlers.MoveNext(out var uid, out var crawling, out var crawler, out var transform, out var sprite))
        {
            if (uid != _players.LocalEntity && (!_container.TryGetContainingContainer(uid, out var container) ||
                (_entity.TryGetComponent<SubFloorHideComponent>(uid, out var hide) && hide.IsUnderCover &&
                !_entity.HasComponent<TrayRevealedComponent>(container.Owner))))
                continue;

            DrawIcon(args, scaleMatrix, rotationMatrix, uid, crawler, transform, sprite);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawIcon(OverlayDrawArgs args, Matrix3x2 scale, Matrix3x2 rotate, EntityUid ent, VentCrawlerComponent crawler, TransformComponent transform, SpriteComponent sprite)
    {
        if (transform.MapID != args.MapId)
            return;

        var bound = _sprite.GetLocalBounds((ent, sprite));

        var worldPos = _transform.GetWorldPosition(transform, _xformQuery);

        if (!bound.Translated(worldPos).Intersects(args.WorldAABB))
            return;

        var handle = args.WorldHandle;

        var worldMatrix = Matrix3x2.CreateTranslation(worldPos);
        var scaledWorld = Matrix3x2.Multiply(scale, worldMatrix);
        var matrix = Matrix3x2.Multiply(rotate, scaledWorld);
        handle.SetTransform(matrix);

        var icon = new Rsi(_rsiPath, crawler.VentCrawlIcon);
        var texture = _sprite.GetFrame(icon, _timing.CurTime);

        //TODO properly center texture
        var position = (bound.Center + sprite.Offset) + new Vector2(-0.5f, -0.5f);
        handle.DrawTexture(texture, position);
    }
}
