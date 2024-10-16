using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Inventory;

[Serializable, NetSerializable]
public enum CMHolsterVisuals
{
    Empty,
    Medium, // TODO: account for the gunslinger belt
    Full,
}
