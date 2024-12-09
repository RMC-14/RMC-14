using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemCamouflageComponent : Component
{
    //you have to add a prototype for each camo type.
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<CamouflageType, EntProtoId> CamouflageVariations = new();
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
