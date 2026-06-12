using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class AutodocComponent : Component
{
    [DataField]
    public string ContainerId = "autodoc";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    /// <summary>
    /// The prototype to spawn the console. If null, no console is spawned.
    /// </summary>
    [DataField]
    public EntProtoId<AutodocConsoleComponent>? SpawnConsolePrototype = "RMCAutodocConsole";

    /// <summary>
    /// Offset for spawning the console relative to the autodoc.
    /// This is applied based on the autodoc's rotation.
    /// </summary>
    [DataField]
    public Vector2 ConsoleSpawnOffset = new(-1, 0);

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> SkillRequired = new() { ["RMCSkillSurgery"] = 1 };

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool IsSurgeryInProgress;

    [DataField, AutoNetworkedField]
    public AutodocSurgeryType CurrentSurgeryType;

    /// <summary>
    /// Delay between processing ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Time of next processing tick.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SurgeryCompleteAt;

    #region External Treatments (Continuous)

    [DataField, AutoNetworkedField]
    public bool HealingBrute;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BruteHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public bool HealingBurn;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BurnHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public bool HealingToxin;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ToxinHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public bool BloodTransfusion;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodTransfusionAmount = FixedPoint2.New(9); // Double iv stand rate

    [DataField, AutoNetworkedField]
    public bool Filtering;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] NonTransferableReagents = ["Blood"];

    #endregion

    #region Surgical Procedures (Queued)

    [DataField, AutoNetworkedField]
    public bool RemoveLarva;

    [DataField, AutoNetworkedField]
    public bool CloseIncisions;

    [DataField, AutoNetworkedField]
    public bool RemoveShrapnel;

    [DataField, AutoNetworkedField]
    public bool InternalBleeding;

    [DataField, AutoNetworkedField]
    public bool BrokenBone;

    [DataField, AutoNetworkedField]
    public bool OrganDamage;

    #endregion

    #region Surgery Step Durations

    [DataField]
    public TimeSpan UnneededDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan IncisionManagerDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan ScalpelDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan RetractorDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan HemostatDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan CircularSawDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan CauteryDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan BoneGelDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan RemoveObjectDuration = TimeSpan.FromSeconds(6);

    #endregion

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier AutoEjectDeadSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [DataField]
    public SoundSpecifier SurgeryCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier SurgeryStepSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}

public enum AutodocSurgeryType : byte
{
    None = 0,
    LarvaExtraction,
    CloseIncision,
    ShrapnelRemoval,
    InternalBleeding,
    BrokenBone,
    OrganDamage,
}
