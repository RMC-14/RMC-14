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
    /// If true, the entity containing this component will ignore most collisions except for Impassable fixture layers.
    /// This is granted to a fired entity by the ShootAtFixedPointComponent based on its ShootArcProj boolean.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ArcProj = false;
}
