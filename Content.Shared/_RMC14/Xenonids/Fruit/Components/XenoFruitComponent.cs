using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared._RMC14.Xenonids.Fruit.Events;

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
    public string EatenState = "fruit_lesser_spent";

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

    // Fruit harvest do-after delay
    [DataField, AutoNetworkedField]
    public TimeSpan HarvestDelay = TimeSpan.FromSeconds(1.5f);

    // Fruit consumption do-after delay
    [DataField, AutoNetworkedField]
    public TimeSpan ConsumeDelay = TimeSpan.FromSeconds(5);

    // Can this fruit be consumed at full health?
    [DataField, AutoNetworkedField]
    public bool CanConsumeAtFull = true;

    // Popup to display upon consumption
    [DataField, AutoNetworkedField]
    public LocId Popup = new LocId("rmc-xeno-fruit-effect-lesser");

    // Color for the gardener overlay
    [DataField, AutoNetworkedField]
    public Color? Color;

    // Color for the aura overlay
    [DataField, AutoNetworkedField]
    public Color OutlineColor;

    [DataField, AutoNetworkedField]
    public float SpentDespawnTime = 1.0f;
}

[Serializable, NetSerializable]
public enum XenoFruitState
{
    Item,
    Growing,
    Grown,
    Eaten
}

[Serializable, NetSerializable]
public enum XenoFruitLayers
{
    Base
}
