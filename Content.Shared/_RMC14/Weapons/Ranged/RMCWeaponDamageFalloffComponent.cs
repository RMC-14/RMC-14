using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCWeaponDamageFalloffComponent : Component
{
    /// <summary>
    /// This is the base modifier for all fired projectiles' effective range.
    /// Conversion from 13: effective_range_max
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EffectiveRange = 0f;

    [DataField, AutoNetworkedField]
    public float ModifiedEffectiveRange = 0f;

    /// <summary>
    /// This is the baase multiplier applied the all fired projectiles' falloff.
    /// Conversion from 13: damage_falloff_mult
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FalloffMultiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ModifiedFalloffMultiplier = 1;
}
