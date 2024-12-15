using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent]
public sealed partial class ItemCamouflageComponent : Component
{
}

[Serializable, NetSerializable]
public enum CamouflageType : byte
{
    Jungle = 1, //default
    Desert = 2,
    Snow = 3,
    Classic = 4,
    Urban = 5,
}

[Serializable, NetSerializable]
public enum CamouflageState : byte
{
    Layer
}
