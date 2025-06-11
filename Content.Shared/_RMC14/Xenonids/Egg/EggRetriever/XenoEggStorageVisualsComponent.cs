using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEggStorageVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int FullStates = 3;

    [DataField, AutoNetworkedField]
    public int MaxEggs = 12;
}

[Serializable, NetSerializable]
public enum XenoEggStorageVisualLayers
{
    Base,
}

[Serializable, NetSerializable]
public enum XenoEggStorageVisuals
{
    Active,
    Number
}
