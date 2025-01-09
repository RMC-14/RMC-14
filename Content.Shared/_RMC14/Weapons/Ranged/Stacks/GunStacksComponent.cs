using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunStacksSystem))]
public sealed partial class GunStacksComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IncreaseAP = 10;

    [DataField, AutoNetworkedField]
    public int MaxAP = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageIncrease = FixedPoint2.New(0.2);

    [DataField, AutoNetworkedField]
    public float SetFireRate = 1.4285f;
}
