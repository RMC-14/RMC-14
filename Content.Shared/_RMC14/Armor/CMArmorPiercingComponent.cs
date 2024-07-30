using Robust.Shared.GameStates;
using Content.Shared._RMC14.Weapons.Ranged;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem), typeof(GunAPStacksSystem))]
public sealed partial class CMArmorPiercingComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Amount;
}
