using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class ProjectileFixedDistanceComponent : Component
{
    /// <summary>
    /// Used when firing a FixedDistance to time effectively limit the range.
    /// This component removes itself when CurTime = FlyEndTime to trigger that Event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FlyEndTime;
    /// <summary>
    /// Should projectiles launched by a gun with this component be fired in an 'arc'?
    /// If true, it will ignore most collisions except for Impassable fixture masks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProjectileArcing = false;
}
