using Robust.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitPlanterComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlantRange = 1.9;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> CanPlant = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? FruitChoice;

    [DataField, AutoNetworkedField]
    public TimeSpan PlantDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan HarvestDelay = TimeSpan.FromSeconds(1.5f);

    [DataField, AutoNetworkedField]
    public SoundSpecifier PlantSound = new SoundCollectionSpecifier("RMCResinBuild")
    {
        Params = AudioParams.Default.WithVolume(-10f)
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier HarvestSound = new SoundCollectionSpecifier("XenoResinBreak")
    {
        Params = AudioParams.Default.WithVolume(-10f)
    };

    // Maximum number of fruit allowed for planter
    [DataField, AutoNetworkedField]
    public int MaxFruitAllowed = 3;

    // List of fruit planted by planter
    [DataField, AutoNetworkedField]
    public List<EntityUid> PlantedFruit = new();
}
