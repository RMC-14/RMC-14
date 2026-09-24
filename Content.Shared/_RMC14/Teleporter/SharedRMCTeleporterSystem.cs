using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared.Damage;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Teleporter;

public abstract class SharedRMCTeleporterSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<AlmayerComponent> _almayerQuery;
    private EntityQuery<DropshipComponent> _dropshipQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();
        _almayerQuery = GetEntityQuery<AlmayerComponent>();
        _dropshipQuery = GetEntityQuery<DropshipComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<RMCTeleporterComponent, StartCollideEvent>(OnTeleportStartCollide);
        SubscribeLocalEvent<RMCTeleporterViewerComponent, StartCollideEvent>(OnViewerStartCollide);
        SubscribeLocalEvent<RMCTeleporterViewerComponent, EndCollideEvent>(OnViewerEndCollide);
    }

    private void OnTeleportStartCollide(Entity<RMCTeleporterComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_almayerQuery.HasComp(other) ||
            _dropshipQuery.HasComp(other) ||
            _mapGridQuery.HasComp(other))
        {
            return;
        }

        var otherCoords = _transform.GetMapCoordinates(other);
        var teleporter = _transform.GetMapCoordinates(ent);
        if (otherCoords.MapId != teleporter.MapId)
            return;

        var diff = otherCoords.Position - teleporter.Position;
        if (diff.Length() > 10)
            return;

        teleporter = teleporter.Offset(diff);
        teleporter = teleporter.Offset(ent.Comp.Adjust);

        HandlePulling(other, teleporter);

        if (ent.Comp.TeleportDamage != null)
        {
            _damageableSystem.TryChangeDamage(args.OtherEntity, ent.Comp.TeleportDamage, origin: ent);
        }
    }

    private void OnViewerStartCollide(Entity<RMCTeleporterViewerComponent> ent, ref StartCollideEvent args)
    {
        if (!_actorQuery.TryComp(args.OtherEntity, out var actor))
            return;

        var query = EntityQueryEnumerator<RMCTeleporterViewerComponent>();
        while (query.MoveNext(out var uid, out var viewer))
        {
            if (uid == ent.Owner || viewer.Id != ent.Comp.Id)
                continue;

            AddViewer((uid, viewer), actor.PlayerSession);
        }
    }

    private void OnViewerEndCollide(Entity<RMCTeleporterViewerComponent> ent, ref EndCollideEvent args)
    {
        if (!_actorQuery.TryComp(args.OtherEntity, out var actor))
            return;

        var query = EntityQueryEnumerator<RMCTeleporterViewerComponent>();
        while (query.MoveNext(out var uid, out var viewer))
        {
            if (uid == ent.Owner || viewer.Id != ent.Comp.Id)
                continue;

            RemoveViewer((uid, viewer), actor.PlayerSession);
        }
    }

    protected virtual void AddViewer(Entity<RMCTeleporterViewerComponent> viewer, ICommonSession player)
    {
    }

    protected virtual void RemoveViewer(Entity<RMCTeleporterViewerComponent> viewer, ICommonSession player)
    {
    }

    public IEnumerable<Entity<RMCTeleporterViewerComponent>> GetMatchingTeleporterViewers(Entity<RMCTeleporterViewerComponent> viewer)
    {
        var viewers = EntityQueryEnumerator<RMCTeleporterViewerComponent>();
        while (viewers.MoveNext(out var otherUid, out var otherViewer))
        {
            if (viewer.Owner != otherUid && viewer.Comp.Id == otherViewer.Id)
                yield return (otherUid, otherViewer);
        }
    }

    public void HandlePulling(EntityUid user, MapCoordinates teleport)
    {
        if (TryComp(user, out PullableComponent? otherPullable) &&
            otherPullable.Puller != null)
        {
            _pulling.TryStopPull(user, otherPullable, otherPullable.Puller.Value);
        }

        if (TryComp(user, out PullerComponent? puller) &&
            TryComp(puller.Pulling, out PullableComponent? pullable))
        {
            if (TryComp(puller.Pulling, out PullerComponent? otherPullingPuller) &&
                TryComp(otherPullingPuller.Pulling, out PullableComponent? otherPullingPullable))
            {
                _pulling.TryStopPull(otherPullingPuller.Pulling.Value, otherPullingPullable, puller.Pulling);
            }

            var pulling = puller.Pulling.Value;
            _pulling.TryStopPull(pulling, pullable, user);
            _transform.SetMapCoordinates(user, teleport);
            _transform.SetMapCoordinates(pulling, teleport);
            _pulling.TryStartPull(user, pulling);
        }
        else
        {
            _transform.SetMapCoordinates(user, teleport);
        }
    }
}
