using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Inventory;

[Serializable, NetSerializable]
public enum CMItemSlotsVisuals
{
    Empty,
    Low,
    Medium,
    High,
    Full,
}
