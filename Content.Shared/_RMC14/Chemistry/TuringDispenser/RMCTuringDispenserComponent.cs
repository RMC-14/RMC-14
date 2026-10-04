using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.TuringDispenser;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCTuringDispenserSystem))]
public sealed partial class RMCTuringDispenserComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Energy;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxEnergy = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RechargeAmount = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan RechargeEvery = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRecharge;

    [DataField, AutoNetworkedField]
    public TimeSpan ProcessEvery = TimeSpan.FromSeconds(1.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextProcess;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPerUnit = 0.1;

    [DataField, AutoNetworkedField]
    public string InputBoxSlotId = "turing_input_box_slot";

    [DataField, AutoNetworkedField]
    public string OutputBeakerSlotId = "turing_output_beaker_slot";

    [DataField, AutoNetworkedField]
    public string BufferSolution = "buffer";

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxBufferVolume = 1080;

    [DataField, AutoNetworkedField]
    public List<TuringProgramEntry> MemoryProgram = new();

    [DataField, AutoNetworkedField]
    public List<TuringProgramEntry> BoxProgram = new();

    [DataField, AutoNetworkedField]
    public TuringDispenserProgram ActiveProgram = TuringDispenserProgram.Box;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1;

    [DataField, AutoNetworkedField]
    public int CycleLimit = 1;

    [DataField, AutoNetworkedField]
    public int Cycle;

    [DataField, AutoNetworkedField]
    public int Stage;

    [DataField, AutoNetworkedField]
    public FixedPoint2 StageMissing;

    [DataField, AutoNetworkedField]
    public TuringDispenserStatus Status = TuringDispenserStatus.Idle;

    [DataField, AutoNetworkedField]
    public string? Error;

    [DataField, AutoNetworkedField]
    public TimeSpan StuckRetryDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextStuckRetry;

    [DataField, AutoNetworkedField]
    public bool AutoRun;

    [DataField, AutoNetworkedField]
    public bool SmartLink = true;

    [DataField, AutoNetworkedField]
    public TuringDispenserOutputMode OutputMode = TuringDispenserOutputMode.Container;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ReagentPrototype>> SynthesizableReagents =
    [
        "RMCAluminum", "RMCCarbon", "RMCChlorine", "RMCCopper", "RMCEthanol", "RMCFluorine",
        "RMCHydrogen", "RMCIron", "RMCLithium", "RMCMercury", "RMCNitrogen", "RMCOxygen",
        "RMCPhosphorus", "RMCPotassium", "RMCRadium", "RMCSilicon", "RMCSodium", "RMCSugar",
        "RMCSulfur", "RMCSulphuricAcid", "Water",
    ];

    [DataField, AutoNetworkedField]
    public float SmartFridgeRange = 3;

    [DataField, AutoNetworkedField]
    public float CentrifugeRange = 20;

    [DataField, AutoNetworkedField]
    public EntProtoId? PreferredBeaker;

    public HashSet<EntityUid> FlushedContainers = new();
}
