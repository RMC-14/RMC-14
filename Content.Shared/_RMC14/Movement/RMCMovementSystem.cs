using System.Linq;
using Content.Shared.Climbing.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCMovementSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const CollisionGroup ClimbCheckGroup = CollisionGroup.Impassable | CollisionGroup.HighImpassable |
                                              CollisionGroup.MidImpassable | CollisionGroup.LowImpassable;

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

        var userPosition = _transform.GetMoverCoordinates(user.Value).Position;
        var targetPosition = _transform.GetMoverCoordinates(target).Position;
        var direction = targetPosition - userPosition;
        var ray = new CollisionRay(userPosition, direction.Normalized(), (int)ClimbCheckGroup);
        var intersect = _physics.IntersectRayWithPredicate(Transform(user.Value).MapID, ray, direction.Length(), e => !Transform(e).Anchored);
        var results = intersect.Select(r => r.HitEntity).ToHashSet();

        if (!includeTarget)
            results.Remove(target);

        foreach (var entity in results)
        {
            var ev = new AttemptClimbEvent(user.Value, movingEntity, entity);
            RaiseLocalEvent(entity, ref ev);
            if (!ev.Cancelled)
            {
                continue;
            }

            if (popup && !ev.PopupHandled)
            {
                _popup.PopupClient(Loc.GetString("rmc-climb-prevented-by-obstacles"), user, PopupType.MediumCaution);
            }
            return false;
        }
        return true;
    }
}
