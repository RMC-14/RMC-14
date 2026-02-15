using Content.Shared._RMC14.Xenonids.Projectile;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem), typeof(XenoProjectileSystem))]
public sealed partial class ProjectileFixedDistanceComponent : Component
{
    /// <summary>
    /// Used when firing a FixedDistance to time effectively limit the range.
    /// This component removes itself when CurTime = FlyEndTime to trigger that Event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FlyEndTime;

    /// <summary>
    /// Optional exact stop position for fixed-distance shots.
    /// When set, the projectile will snap here when it reaches its stop time to avoid drift.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MapCoordinates? TargetCoordinates;
    /// <summary>
    /// If true, the entity containing this component will ignore most collisions except for Impassable fixture layers.
    /// This is granted to a fired entity by the ShootAtFixedPointComponent based on its ShootArcProj boolean.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ArcProj = false;
}
