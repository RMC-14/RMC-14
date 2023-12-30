using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
public sealed partial class CMAmmoBoxComponent : Component
{
    [DataField]
    public Enum AmmoLayer = CMAmmoBoxLayers.Ammo;
}

[Serializable, NetSerializable]
public enum CMAmmoBoxLayers
{
    Ammo
}
