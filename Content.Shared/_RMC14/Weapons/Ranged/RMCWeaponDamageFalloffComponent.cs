using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCWeaponDamageFalloffComponent : Component
{
    /// <summary>
    /// This is the baase multiplier applied the all fired projectiles' falloff.
    /// Conversion from 13: damage_falloff_mult
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FalloffMultiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ModifiedFalloffMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float RangeFlat;

    [DataField, AutoNetworkedField]
    public float RangeFlatModified;
}
