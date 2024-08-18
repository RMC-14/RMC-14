using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Teleporter;

public abstract class SharedRMCTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
        if (_almayerQuery.HasComp(args.OtherEntity) ||
            _dropshipQuery.HasComp(args.OtherEntity) ||
            _mapGridQuery.HasComp(args.OtherEntity))
        {
            return;
        }

        var user = _transform.GetMapCoordinates(args.OtherEntity);
        var teleporter = _transform.GetMapCoordinates(ent);
        if (user.MapId != teleporter.MapId)
            return;

        var diff = user.Position - teleporter.Position;
        if (diff.Length() > 10)
            return;

        teleporter = teleporter.Offset(diff);
        teleporter = teleporter.Offset(ent.Comp.Adjust);
        _transform.SetMapCoordinates(args.OtherEntity, teleporter);
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
}
