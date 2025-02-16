using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class UserDamageOverTimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DamageEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan NextDamageAt;

    [DataField, AutoNetworkedField]
    public CollisionGroup Collision = CollisionGroup.MobLayer | CollisionGroup.MobMask;
}
