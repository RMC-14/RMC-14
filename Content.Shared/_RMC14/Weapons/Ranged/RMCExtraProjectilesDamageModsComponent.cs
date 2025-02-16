using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCExtraProjectilesDamageModsComponent : Component
{
    /// <summary>
    /// This modifier is applied to all projectiles past the first when firing multiple in one shot. Such as with buckshot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageMultiplier = 1;
}
