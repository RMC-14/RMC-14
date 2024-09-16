using Robust.Shared.Audio;
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
    public SoundSpecifier HarvestSound = new SoundCollectionSpecifier("XenoResinBreak")
    {
        Params = AudioParams.Default.WithVolume(-10f)
    };

    [DataField, AutoNetworkedField]
    public EntityUid? Hive;

    // entity who planted the given fruit
    [DataField, AutoNetworkedField]
    public EntityUid? Planter;

    // Is this fruit being harvested by any xeno?
    [DataField, AutoNetworkedField]
    public bool IsHarvested = false;

    // Is this fruit being consumed by any xeno?
    [DataField, AutoNetworkedField]
    public bool IsPicked = false;

    // Fruit harvest do-after delay
    [DataField, AutoNetworkedField]
    public TimeSpan HarvestDelay = TimeSpan.FromSeconds(1.5f);

    // Fruit consumption do-after delay
    [DataField, AutoNetworkedField]
    public TimeSpan ConsumeDelay = TimeSpan.FromSeconds(5);

    // Can this fruit be consumed at full health?
    [DataField, AutoNetworkedField]
    public bool CanConsumeAtFull = true;

    // Components to add for the duration
    [DataField(required: true)]
    public ComponentRegistry Effects = new();
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
