using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Inventory;

[Serializable, NetSerializable]
public enum CMItemSlotsVisuals
{
    Empty,
    Low,
    Medium,
    High,
    Full,
}
