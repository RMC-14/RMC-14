using System.Numerics;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Sleeper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperComponent : Component
{
    [DataField]
    public string ContainerId = "sleeper";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    /// <summary>
    /// The prototype to spawn the console. If null, no console is spawned.
    /// </summary>
    [DataField]
    public EntProtoId<SleeperConsoleComponent>? SpawnConsolePrototype = "RMCSleeperConsole";

    /// <summary>
    /// Offset for spawning the console relative to the sleeper.
    /// This is applied based on the sleeper's rotation.
    /// </summary>
    [DataField]
    public Vector2 ConsoleSpawnOffset = new(1, 0);

    /// <summary>
    /// List of chemicals available to inject.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] AvailableChemicals =
    [
        "CMInaprovaline",
        //"RMCParacetamol",
        "CMDylovene",
        "CMDexalin",
        "CMTricordrazine"
    ];

    /// <summary>
    /// Additional chemicals that become available when in emergency mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] EmergencyChemicals =
    [
        //"RMCOxycodone",
        "CMBicaridine",
        "CMKelotane"
    ];

    [DataField, AutoNetworkedField]
    public int[] InjectionAmounts = [5, 10];

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxChemical = 40;

    /// <summary>
    /// How much remaining health in percentage for triggering emergency mode.
    /// </summary>
    /// <example>0.4f means emergency mode will trigger when health drops below 40%.</example>
    [DataField, AutoNetworkedField]
    public float PercentHealthThreshold = 0.5f;

    [DataField, AutoNetworkedField]
    public bool IsFiltering;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisAmount = FixedPoint2.New(3);

    /// <summary>
    /// Delay between dialysis ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DialysisTickDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time of next dialysis tick.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDialysisTick;

    /// <summary>
    /// Total reagent volume when dialysis started (for progress tracking).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisStartedReagentVolume;

    /// <summary>
    /// Reagents that cannot be removed by dialysis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] NonTransferableReagents = ["Blood"];

    [DataField, AutoNetworkedField]
    public bool AutoEjectDead;

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    [DataField, AutoNetworkedField]
    public EntityUid? SpawnedConsole;

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier AutoEjectDeadSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [DataField]
    public SoundSpecifier DialysisCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
