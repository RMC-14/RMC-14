using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class ShootAtFixedPointComponent : Component // TODO: Make it so weapons with this component can have arc fire disabled.
{
    /// <summary>
    /// Sets the maximum range for a projectile fired with ShootAtFixedPointComponent.
    /// This can be set on both the Projectile and ShootAtFixedPoint Components.
    /// The default value is null for no cap. The minimum value between the two is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? MaxFixedRange;

    /// <summary>
    /// If true, targetting past an Impassable fixture obstacle result in automatically targetting
    /// right in front of the obstacle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoAimClosestObstacle = false;

    /// <summary>
    /// Should projectiles launched by a gun with this component be fired in an 'arc'?
    /// If true, they will ignore most collisions except for Impassable fixture layers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShootArcProj = false;
}
