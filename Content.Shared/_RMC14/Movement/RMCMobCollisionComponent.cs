using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared._RMC14.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMovementSystem))]
public sealed partial class RMCMobCollisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FixtureId = "rmc_mob_collision";

    [DataField, AutoNetworkedField]
    public IPhysShape FixtureShape = new PhysShapeCircle(0.45f);

    [DataField, AutoNetworkedField]
    public CollisionGroup FixtureLayer = CollisionGroup.MobCollision;
}
