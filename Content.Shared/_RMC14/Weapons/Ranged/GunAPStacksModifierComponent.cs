//using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunAPStacksModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public byte Stacks = 0;

    [DataField, AutoNetworkedField]
    public byte AP = 0;

    [DataField, AutoNetworkedField]
    public byte ModifiedStacks;
}
