using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[Serializable, NetSerializable]
public enum DropshipFabricatorVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum DropshipFabricatorState
{
    Idle,
    Fabricating,
}
