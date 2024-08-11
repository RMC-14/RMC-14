using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipWeaponPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_dropship_weapon_point_container";
}
