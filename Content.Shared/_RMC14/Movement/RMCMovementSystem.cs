using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCMovementSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMobCollisionComponent, MapInitEvent>(OnMobCollisionMapInit);
    }

    private void OnMobCollisionMapInit(Entity<RMCMobCollisionComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out MobCollisionComponent? collision))
        {
            collision.FixtureId = ent.Comp.FixtureId;
            DirtyField(ent, collision, nameof(MobCollisionComponent.FixtureId));
        }

        if (!TryComp<PhysicsComponent>(ent, out var body))
            return;

        _fixture.TryCreateFixture(
            ent,
            ent.Comp.FixtureShape,
            ent.Comp.FixtureId,
            hard: false,
            collisionLayer: (int) ent.Comp.FixtureLayer,
            collisionMask: (int) ent.Comp.FixtureLayer,
            body: body
        );
    }
}
