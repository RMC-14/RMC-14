using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemCamouflageComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<CamouflageType, ResPath>? CamouflageVariations;

    [DataField, AutoNetworkedField]
    public Dictionary<CamouflageType, string>? States;

    [DataField, AutoNetworkedField]
    public Dictionary<string, Dictionary<CamouflageType, string>>? Layers;

    [DataField, AutoNetworkedField]
    public Dictionary<CamouflageType, Robust.Shared.Maths.Color>? Colors;
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
