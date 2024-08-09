using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Teleporter;

public sealed class RMCTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AlmayerComponent> _almayerQuery;
    private EntityQuery<DropshipComponent> _dropshipQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _almayerQuery = GetEntityQuery<AlmayerComponent>();
        _dropshipQuery = GetEntityQuery<DropshipComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<RMCTeleporterComponent, StartCollideEvent>(OnTeleportStartCollide);
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
}
