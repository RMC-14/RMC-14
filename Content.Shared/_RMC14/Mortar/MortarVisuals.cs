using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mortar;

[Serializable, NetSerializable]
public enum MortarVisuals
{
    Item,
    Deployed,
}

[Serializable, NetSerializable]
public enum MortarVisualLayers
{
    State,
}
