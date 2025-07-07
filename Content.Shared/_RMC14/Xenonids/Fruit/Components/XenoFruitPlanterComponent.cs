using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitPlanterComponent : Component
{
    // Which fruits can this xeno plant?
    [DataField, AutoNetworkedField]
    public List<EntProtoId> CanPlant = new();

    // Currently selected fruit
    [DataField, AutoNetworkedField]
    public EntProtoId? FruitChoice;

    [DataField, AutoNetworkedField]
    public SoundSpecifier PlantSound = new SoundCollectionSpecifier("RMCResinBuild")
    {
        Params = AudioParams.Default.WithVolume(-10f)
    };

    // Maximum number of fruit allowed for planter
    [DataField, AutoNetworkedField]
    public int MaxFruitAllowed = 3;

    // List of fruit planted by planter
    [DataField, AutoNetworkedField]
    public List<EntityUid> PlantedFruit = new();

    [DataField, AutoNetworkedField]
    public float FruitPickingMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float FruitFeedingMultiplier = 1;
}
