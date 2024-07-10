using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class ShootAtFixedPointComponent : Component // TODO: Make it so weapons with this component can have arc fire disabled.
{
    /// <summary>
    /// Sets the maximum range for a projectile fired with ShootAtFixedPointComponent.
    /// This can be set on both the Projectile and ShootAtFixedPoint Components.
    /// The default value is 0 for no cap. The minimum nonzero value between the two is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double MaxFixedRange = 0;

    /// <summary>
    /// Should projectiles launched by a gun with this component be fired in an 'arc'?
    /// If true, it will ignore most collisions except for Impassable fixture masks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShootArc = false;

    // Rename this to ShootAtPoint maybe? ShootAtFixedPoint makes it sound like this goes on an AmmoComponent entit to give it a fixed range it'll always go to.
}