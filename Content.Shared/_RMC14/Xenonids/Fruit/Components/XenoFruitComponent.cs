using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoFruitState State = XenoFruitState.Growing;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? GrowAt;

    [DataField, AutoNetworkedField]
    public TimeSpan GrowTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public string ItemState = "fruit_lesser_item";

    [DataField, AutoNetworkedField]
    public string GrowingState = "fruit_lesser_immature";

    [DataField, AutoNetworkedField]
    public string GrownState = "fruit_lesser";

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPlasma = 100;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier CostHealth = default!;

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
