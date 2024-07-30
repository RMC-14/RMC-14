using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCProjectileDamageFalloffComponent : Component
{
    /// <summary>
    /// The maximum distance at which the projectile deals its full damage.
    /// Conversion from 13: effective_range_max
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EffectiveRange = 0f;

    /// <summary>
    /// This is the number by which the projectile's damage is decreased for each tile travelled beyond its effective range.
    /// Conversion from 13: damage_falloff
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Falloff = 1;

    /// <summary>
    /// This determines the minimum fraction of the projectile's original damage that must remain after falloff.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinRemainingDamageMult = 0.1f;

    /// <summary>
    /// These are the coordinates from which the projectile was shot. Used to determine the distance travelled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MapCoordinates? ShotFrom;
}
