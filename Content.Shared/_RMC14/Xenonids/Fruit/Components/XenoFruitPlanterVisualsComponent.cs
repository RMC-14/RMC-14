using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoFruitPlanterVisualsSystem))]
public sealed partial class XenoFruitPlanterVisualsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Rsi;

    [DataField(required: true), AutoNetworkedField]
    public string Prefix;

    [DataField, AutoNetworkedField]
    public Color? Color;
}

[Serializable, NetSerializable]
public enum XenoFruitPlanterVisuals
{
    Resting,
    Downed,
    Color,
}

[Serializable, NetSerializable]
public enum XenoFruitVisualLayers
{
    Base,
}
