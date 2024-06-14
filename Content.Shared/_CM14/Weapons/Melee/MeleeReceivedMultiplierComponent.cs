using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMMeleeWeaponSystem))]
public sealed partial class MeleeReceivedMultiplierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier XenoDamage; // TODO CM14 other hives

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 OtherMultiplier;
}
