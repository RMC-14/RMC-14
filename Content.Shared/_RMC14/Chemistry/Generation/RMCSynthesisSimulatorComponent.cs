using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCSynthesisSimulatorSystem))]
public sealed partial class RMCSynthesisSimulatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string TargetSlotId = "synthesis_simulator_target_slot";

    [DataField, AutoNetworkedField]
    public string ReferenceSlotId = "synthesis_simulator_reference_slot";

    [DataField, AutoNetworkedField]
    public SynthesisMode Mode = SynthesisMode.Amplify;

    [DataField, AutoNetworkedField]
    public ProtoId<ChemGeneratorPropertyPrototype>? TargetProperty;

    [DataField, AutoNetworkedField]
    public ProtoId<ChemGeneratorPropertyPrototype>? ReferenceProperty;

    [DataField, AutoNetworkedField]
    public bool Simulating;

    [DataField, AutoNetworkedField]
    public bool Picking;

    [DataField, AutoNetworkedField]
    public bool SimulationFailed;

    [DataField, AutoNetworkedField]
    public List<List<RecipeCandidateIngredient>>? RecipeCandidates;

    [DataField]
    public List<ChemReportProperty>? PendingProperties;

    [DataField]
    public int PendingOverdose;

    [DataField]
    public int PendingTier;

    [DataField]
    public List<string?>? PendingNewIngredientIds;

    [DataField]
    public string? PendingSignature;

    [DataField, AutoNetworkedField]
    public int PendingCost;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FinishAt;

    [DataField]
    public TimeSpan? UnpoweredRemaining;

    [DataField, AutoNetworkedField]
    public TimeSpan ProcessDuration = TimeSpan.FromSeconds(6);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? ReadingUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? PrintingUntil;

    [DataField, AutoNetworkedField]
    public TimeSpan ReadingDuration = TimeSpan.FromSeconds(0.8);

    [DataField, AutoNetworkedField]
    public TimeSpan PrintingDuration = TimeSpan.FromSeconds(0.8);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FinishSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FailSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/buzz_two.ogg");

    [DataField, AutoNetworkedField]
    public int TechtreeLevelMultiplier = 2;

    [DataField, AutoNetworkedField]
    public int MaxClearanceLevel = 5;

    [DataField, AutoNetworkedField]
    public int PropertyCostMax = 8;

    [DataField, AutoNetworkedField]
    public int AnomalousMultiplier = 5;

    [DataField, AutoNetworkedField]
    public int RareMultiplier = 3;

    [DataField, AutoNetworkedField]
    public int AddBulkMultiplier = 2;

    [DataField, AutoNetworkedField]
    public int AddValueMultiplier = 3;

    [DataField, AutoNetworkedField]
    public int AddBulkThreshold = 4;

    [DataField, AutoNetworkedField]
    public int AddValueThreshold = 7;

    [DataField, AutoNetworkedField]
    public int RelateAnomalousMultiplier = 10;

    [DataField, AutoNetworkedField]
    public int NonPositiveDiscount = 2;

    [DataField]
    public int RecipeCandidateCount = 3;

    [DataField]
    public int RecipeRollAttempts = 8;
}

[Serializable, DataDefinition]
public sealed partial class RecipeCandidateIngredient
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public int Amount;

    [DataField]
    public bool Catalyst;
}
