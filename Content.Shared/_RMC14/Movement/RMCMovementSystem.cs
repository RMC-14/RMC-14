using Content.Shared._RMC14.Line;
using Content.Shared.Climbing.Events;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCMovementSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;

    private HashSet<EntityUid> _intersectedEntities = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMobCollisionComponent, MapInitEvent>(OnMobCollisionMapInit);
        SubscribeLocalEvent<RMCMobCollisionComponent, MobStateChangedEvent>(OnMobCollisionMobStateChanged);
    }

    private void OnMobCollisionMapInit(Entity<RMCMobCollisionComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out MobCollisionComponent? collision))
        {
            collision.FixtureId = ent.Comp.FixtureId;
            DirtyField(ent, collision, nameof(MobCollisionComponent.FixtureId));
        }

        if (_mobState.IsDead(ent))
            return;

        if (!TryComp<PhysicsComponent>(ent, out var body))
            return;

        CreateMobCollisionFixture((ent, ent, body));
    }

    private void CreateMobCollisionFixture(Entity<RMCMobCollisionComponent, PhysicsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        _fixture.TryCreateFixture(
            ent,
            ent.Comp1.FixtureShape,
            ent.Comp1.FixtureId,
            hard: false,
            collisionLayer: (int) ent.Comp1.FixtureLayer,
            collisionMask: (int) ent.Comp1.FixtureLayer,
            body: ent
        );
    }

    private void OnMobCollisionMobStateChanged(Entity<RMCMobCollisionComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Alive:
                CreateMobCollisionFixture(ent);
                break;
            case MobState.Dead:
                _fixture.DestroyFixture(ent, ent.Comp.FixtureId);
                break;
        }
    }

    public bool CanClimbOver(EntityUid? user, EntityUid movingEntity, EntityUid target, bool includeTarget = true, bool popup = true)
    {
        if (user is null)
        {
            user = movingEntity;
        }

        var movingEntityMapCoords = _transform.GetMapCoordinates(movingEntity);
        var targetMapCoords = _transform.GetMapCoordinates(target);
        var transform = new Transform(0);

        var line = new EdgeShape(movingEntityMapCoords.Position, targetMapCoords.Position);

        _intersectedEntities.Clear();
        _lookup.GetEntitiesIntersecting<EdgeShape>(_transform.GetMapId(movingEntity), line, transform, _intersectedEntities);

        if (includeTarget)
            _intersectedEntities.Add(target);
        else
            _intersectedEntities.Remove(target);

        foreach (var entity in _intersectedEntities)
        {
            var ev = new AttemptClimbEvent(user.Value, movingEntity, entity);
            RaiseLocalEvent(entity, ref ev);
            if (!ev.Cancelled)
            {
                continue;
            }

            if (popup)
            {
                _popup.PopupClient(Loc.GetString("rmc-climb-prevented-by-obstacles"), user, PopupType.MediumCaution);
            }
            return false;
        }
        return true;
    }
}
