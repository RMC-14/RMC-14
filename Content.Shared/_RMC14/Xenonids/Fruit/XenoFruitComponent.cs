using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoFruitState State;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? GrowAt;

    [DataField, AutoNetworkedField]
    public string ItemState = "fruit_lesser_item";

    [DataField, AutoNetworkedField]
    public string GrowingState = "fruit_lesser_immature";

    [DataField, AutoNetworkedField]
    public string GrownState = "fruit_lesser";

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPlasma = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostHealth = 50;

    [DataField, AutoNetworkedField]
    public EntityUid? Hive;

    // entity who planted the given fruit
    [DataField, AutoNetworkedField]
    public EntityUid? Planter;
}

[Serializable, NetSerializable]
public enum XenoFruitState
{
    Item,
    Growing,
    Grown
}

[Serializable, NetSerializable]
public enum XenoFruitLayers
{
    Base
}
