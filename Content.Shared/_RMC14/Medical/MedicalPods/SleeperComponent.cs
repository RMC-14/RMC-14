using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.MedicalPods;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperComponent : Component
{
    /// <summary>
    /// The container ID for the occupant.
    /// </summary>
    [DataField]
    public string ContainerId = "sleeper";

    /// <summary>
    /// The current occupant of the sleeper.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    /// <summary>
    /// List of chemicals available to inject.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>> AvailableChemicals = new()
    {
        "RMCInaprovaline",
        "RMCParacetamol",
        "RMCDylovene",
        "RMCDexalin",
        "RMCTricordrazine"
    };

    /// <summary>
    /// List of chemicals that can be injected when occupant health is critical.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>> EmergencyChemicals = new()
    {
        "RMCInaprovaline",
        "RMCParacetamol",
        "RMCDylovene",
        "RMCDexalin",
        "RMCTricordrazine",
        "RMCOxycodone",
        "RMCBicaridine",
        "RMCKelotane"
    };

    /// <summary>
    /// Amount options for chemical injection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<int> InjectionAmounts = new() { 5, 10 };

    /// <summary>
    /// Maximum amount of any single chemical allowed in occupant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxChemical = 40;

    /// <summary>
    /// Minimum health for normal chemical injection (below this, only emergency chems work).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinHealth = 10f;

    /// <summary>
    /// Whether dialysis is currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Filtering;

    /// <summary>
    /// Reagent removal amount per second during dialysis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReagentRemovalRate = FixedPoint2.New(3);

    /// <summary>
    /// Reagents that cannot be removed by dialysis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>> NonTransferableReagents = new() { "Blood" };

    /// <summary>
    /// Whether to automatically eject occupant on death.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoEjectDead;

    /// <summary>
    /// Duration of stun when exiting the sleeper.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Delay for entering the sleeper.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EntryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Delay for pushing someone else into the sleeper.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PushInDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Skills required to operate the sleeper.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SkillWhitelist? SkillRequired;

    /// <summary>
    /// The linked console entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Time of next dialysis tick.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDialysisTick;

    /// <summary>
    /// Delay between dialysis ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DialysisTickDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Total reagent volume when dialysis started (for progress tracking).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisStartedReagentVolume;
}
