//using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunAPStacksModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stacks = 0;

    [DataField, AutoNetworkedField]
    public int AP = 0;

    [DataField, AutoNetworkedField]
    public int ModifiedStacks;
}
