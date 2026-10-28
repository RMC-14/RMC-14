using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(RMCPlantAnalyzerSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class RMCPlantAnalyzerComponent : Component
{
    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public float SensitiveToleranceThreshold = 3f;

    [DataField]
    public float ResistantToleranceThreshold = 6f;

    [DataField]
    public float TrayLevelMax = 100f;

    [DataField]
    public float TrayHazardMax = 10f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    public EntityUid? User;

    [AutoNetworkedField]
    public EntityUid? Target;

    [AutoNetworkedField]
    public EntityUid? DisplayEntity;

    [AutoNetworkedField]
    public string PlantName = "";

    [AutoNetworkedField]
    public bool IsTray;

    [AutoNetworkedField]
    public bool TrayEmpty;

    [AutoNetworkedField]
    public bool IsProduce;

    [AutoNetworkedField]
    public bool HasPlantData;

    [AutoNetworkedField]
    public float Health;

    [AutoNetworkedField]
    public float Endurance = 100f;

    [AutoNetworkedField]
    public int Age;

    [AutoNetworkedField]
    public float Maturation;

    [AutoNetworkedField]
    public bool ReadyToHarvest;

    [AutoNetworkedField]
    public int Yield;

    [AutoNetworkedField]
    public float Potency;

    [AutoNetworkedField]
    public float Lifespan;

    [AutoNetworkedField]
    public float Production;

    [AutoNetworkedField]
    public HarvestType HarvestRepeat;

    [AutoNetworkedField]
    public bool Seedless;

    [AutoNetworkedField]
    public bool Viable = true;

    [AutoNetworkedField]
    public float WaterLevel;

    [AutoNetworkedField]
    public float NutritionLevel;

    [AutoNetworkedField]
    public float PestLevel;

    [AutoNetworkedField]
    public float WeedLevel;

    [AutoNetworkedField]
    public float Toxins;

    [AutoNetworkedField]
    public float MinHeat;

    [AutoNetworkedField]
    public float MaxHeat;

    [AutoNetworkedField]
    public float MinPressure;

    [AutoNetworkedField]
    public float MaxPressure;

    [AutoNetworkedField]
    public List<string> Traits = new();

    [AutoNetworkedField]
    public List<RMCPlantAnalyzerChemical> Chemicals = new();
}
