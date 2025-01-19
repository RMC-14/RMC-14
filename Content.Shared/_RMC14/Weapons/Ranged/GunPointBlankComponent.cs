using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunPointBlankComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ProneDamageMult = 1.2;

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;
}
