using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class AmmoFixedDistanceComponent : Component
{
    /// <summary>
    /// This will be applied to the projectiles fired by the weapon.
    /// </summary>
    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>)), AutoNetworkedField]
    public int CollisionLayer = (int) CollisionGroup.ThrownItem;
}
