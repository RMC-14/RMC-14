using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.XenoTransformation;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCXenoTransformationComponent : Component
{
    [DataField]
    public int Stage = 1;

    [DataField]
    public int MaxStage = 5;

    [DataField]
    public int Age;

    [DataField]
    public int StageMinimumAge = 180;

    [DataField]
    public bool Carrier;

    [DataField]
    public float CarrierChance = 0.05f;

    [DataField]
    public int Longevity = 150;

    [DataField]
    public float LongevityDecayChance = 0.7f;

    [DataField]
    public bool Started;

    [DataField]
    public bool HivemindGranted;

    [DataField]
    public EntityUid? Hive;

    [DataField]
    public TimeSpan TickEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;

    [DataField]
    public List<EntProtoId> Castes = new() { "CMXenoDrone", "CMXenoRunner", "CMXenoSentinel", "CMXenoDefender" };
}
