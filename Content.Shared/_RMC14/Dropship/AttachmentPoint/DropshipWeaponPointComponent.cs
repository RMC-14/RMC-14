using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

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

    [DataField, AutoNetworkedField]
    public DropshipWeaponPointLocation? Location;
}

[Serializable, NetSerializable]
public enum DropshipWeaponPointLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum DropshipWeaponPointLocation
{
    StarboardFore,
    PortFore,
    StarboardWing,
    PortWing,
}
