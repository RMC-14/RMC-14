using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Inventory;

[Serializable, NetSerializable]
public enum CMHolsterLayers
{
    Fill,   // For displaying the weapon underlay
    Size,   // For Storage-based holsters to not display as filled when containing only a weapon
}
