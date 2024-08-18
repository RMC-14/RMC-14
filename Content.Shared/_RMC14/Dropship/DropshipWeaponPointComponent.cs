using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipWeaponPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string WeaponContainerSlotId = "rmc_dropship_weapon_point_weapon_container_slot";

    [DataField, AutoNetworkedField]
    public string AmmoContainerSlotId = "rmc_dropship_weapon_point_ammo_container_slot";

    [DataField, AutoNetworkedField]
    public string DirOffset;
}

[Serializable, NetSerializable]
public enum DropshipWeaponPointLayers
{
    Layer,
}
