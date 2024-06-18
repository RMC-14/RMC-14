using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunDamageModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier;
}
