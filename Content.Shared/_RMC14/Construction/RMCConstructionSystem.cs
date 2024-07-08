using Content.Shared._RMC14.Dropship;
using Content.Shared.Construction.Components;
using Content.Shared.Coordinates;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction;

public sealed class RMCConstructionSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId Blocker = "RMCDropshipDoorBlocker";

    private readonly List<EntityCoordinates> _toCreate = new();
    private EntityQuery<DoorComponent> _doorQuery;

    public override void Initialize()
    {
        _doorQuery = GetEntityQuery<DoorComponent>();

        SubscribeLocalEvent<RMCConstructionAttemptEvent>(OnConstructionAttempt);

        SubscribeLocalEvent<DropshipComponent, MapInitEvent>(OnDropshipMapInit);

        SubscribeLocalEvent<RMCDropshipBlockedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCDropshipBlockedComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<RMCDropshipBlockedComponent, UserAnchoredEvent>(OnUserAnchored);
    }

    private void OnConstructionAttempt(ref RMCConstructionAttemptEvent ev)
    {
        if (_transform.GetGrid(ev.Location) is { } grid &&
            HasComp<DropshipComponent>(grid))
        {
            ev.Popup = Loc.GetString("rmc-construction-not-proper-surface", ("construction", ev.PrototypeName));
            ev.Cancelled = true;
        }
    }

    private void OnDropshipMapInit(Entity<DropshipComponent> ent, ref MapInitEvent args)
    {
        _toCreate.Clear();

        var enumerator = Transform(ent).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_doorQuery.HasComp(child))
                continue;

            _toCreate.Add(child.ToCoordinates());
        }

        foreach (var toCreate in _toCreate)
        {
            SpawnAtPosition(Blocker, toCreate);
        }
    }

    private void OnMapInit(Entity<RMCDropshipBlockedComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out PhysicsComponent? physics))
            return;

        var shape = new PhysShapeCircle(0.49f);
        _fixture.TryCreateFixture(
            ent,
            shape,
            ent.Comp.FixtureId,
            collisionMask: (int) CollisionGroup.DropshipImpassable,
            body: physics
        );
    }

    private void OnAnchorAttempt(Entity<RMCDropshipBlockedComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_transform.GetGrid(ent.Owner) is not { } grid)
            return;

        if (HasComp<DropshipComponent>(grid))
        {
            var msg = Loc.GetString("rmc-construction-not-proper-surface", ("construction", Name(ent)));
            _popup.PopupClient(msg, ent, args.User);
            args.Cancel();
        }
    }

    private void OnUserAnchored(Entity<RMCDropshipBlockedComponent> ent, ref UserAnchoredEvent args)
    {
        if (TryComp(ent.Owner, out TransformComponent? xform))
            _transform.Unanchor(ent.Owner, xform);
    }

    public bool CanConstruct(EntityUid? user)
    {
        return !HasComp<DisableConstructionComponent>(user);
    }
}
