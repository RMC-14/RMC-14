using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client._RMC14.NightVision;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Teleporter;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Systems;
using static Robust.Shared.GameObjects.LookupFlags;

namespace Content.Client._RMC14.Teleporter;

public sealed class RMCTeleporterViewerOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private readonly SharedContainerSystem _container;
    private readonly EntityLookupSystem _entityLookup;
    private readonly SharedPhysicsSystem _physics;
    private readonly SharedRMCTeleporterSystem _teleporter;
    private readonly SharedTransformSystem _transform;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<RMCTeleporterViewerComponent> _teleporterViewerQuery;
    private readonly EntityQuery<TileFireComponent> _tileFireQuery;
    private readonly EntityQuery<TransformComponent> _transformQuery;
    private readonly EntityQuery<XenoComponent> _xenoQuery;

    private readonly List<(Entity<SpriteComponent> Ent, Vector2 Position, Angle Rotation)> _toDraw = new();

    public override OverlaySpace Space => _overlay.HasOverlay<NightVisionOverlay>()
        ? OverlaySpace.WorldSpace
        : OverlaySpace.WorldSpaceBelowFOV;

    public RMCTeleporterViewerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<SharedContainerSystem>();
        _entityLookup = _entity.System<EntityLookupSystem>();
        _physics = _entity.System<SharedPhysicsSystem>();
        _teleporter = _entity.System<SharedRMCTeleporterSystem>();
        _transform = _entity.System<SharedTransformSystem>();

        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _teleporterViewerQuery = _entity.GetEntityQuery<RMCTeleporterViewerComponent>();
        _tileFireQuery = _entity.GetEntityQuery<TileFireComponent>();
        _transformQuery = _entity.GetEntityQuery<TransformComponent>();
        _xenoQuery = _entity.GetEntityQuery<XenoComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;
        foreach (var selfContact in _physics.GetEntitiesIntersectingBody(player, (int) CollisionGroup.MobLayer))
        {
            if (!_teleporterViewerQuery.TryComp(selfContact, out var viewer))
                continue;

            var viewerPosition = _transform.GetWorldPosition(selfContact);
            foreach (var otherViewer in _teleporter.GetMatchingTeleporterViewers((selfContact, viewer)))
            {
                var otherViewerPosition = _transform.GetMapCoordinates(otherViewer);
                var viewerPositionDiff = otherViewerPosition.Position - viewerPosition;
                var otherViewerAABB = _physics.GetWorldAABB(otherViewer);

                _toDraw.Clear();
                foreach (var viewerContact in _entityLookup.GetEntitiesIntersecting(otherViewerPosition.MapId, otherViewerAABB, Uncontained))
                {
                    if (!_spriteQuery.TryComp(viewerContact, out var viewerContactSprite) ||
                        !viewerContactSprite.Visible ||
                        !_transformQuery.TryComp(viewerContact, out var viewerContactTransform))
                    {
                        continue;
                    }

                    if (_container.IsEntityInContainer(viewerContact))
                        continue;

                    if (viewerContactTransform.Anchored &&
                        !_xenoQuery.HasComp(viewerContact) &&
                        !_tileFireQuery.HasComp(viewerContact))
                    {
                        continue;
                    }

                    // technically you could use try delta here for the vast majority of cases
                    // since they share a parent entity uid but i don't got time to do that
                    // good luck future coder
                    var (position, rotation) = _transform.GetWorldPositionRotation(viewerContactTransform);
                    _toDraw.Add(((viewerContact, viewerContactSprite), position, rotation));
                }

                _toDraw.Sort((a, b) => a.Ent.Comp.DrawDepth.CompareTo(b.Ent.Comp.DrawDepth));

                foreach (ref var draw in CollectionsMarshal.AsSpan(_toDraw))
                {
                    draw.Position -= viewerPositionDiff;
                    draw.Ent.Comp.Render(handle, eyeRot, draw.Rotation, position: draw.Position);
                }
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
