using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// High-level destination profile used by restricted RMC shuttle routing.
/// </summary>
public enum RMCShuttleDockingClass
{
    /// <summary>
    /// Normal shuttle profile that docks at standard internal pads.
    /// </summary>
    Standard,

    /// <summary>
    /// Compact shuttle profile allowed to use smaller side or internal pads.
    /// </summary>
    Small,

    /// <summary>
    /// Large shuttle profile that requires external hangar-class pads.
    /// </summary>
    Big,
}

/// <summary>
/// Maps an abstract shuttle profile to concrete landing class tags.
/// </summary>
public static class RMCShuttleDocking
{
    private static readonly string[] SmallDockClasses = ["internal", "external_side"];
    private static readonly string[] StandardDockClasses = ["internal"];
    private static readonly string[] BigDockClasses = ["external_hangar"];

    /// <summary>
    /// Returns the landing classes the shuttle may use for automatic routing and nav-console validation.
    /// </summary>
    public static string[] GetAllowedDockClasses(RMCShuttleDockingClass dockingClass)
    {
        return dockingClass switch
        {
            RMCShuttleDockingClass.Standard => StandardDockClasses,
            RMCShuttleDockingClass.Small => SmallDockClasses,
            RMCShuttleDockingClass.Big => BigDockClasses,
            _ => [],
        };
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
/// <summary>
/// Navigation settings for a dropship flight computer, including restricted destination routing flags.
/// </summary>
public sealed partial class DropshipNavigationComputerComponent : Component
{
    /// <summary>
    /// Skill prototype checked when a player operates this navigation computer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPilot";

    /// <summary>
    /// Minimum skill level required to receive flight multiplier benefits.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MultiplierSkillLevel = 2;

    /// <summary>
    /// Minimum skill level required to perform fly-by actions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FlyBySkillLevel = 2;

    /// <summary>
    /// Speed multiplier applied to fly-by travel for sufficiently skilled pilots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SkillFlyByMultiplier = 1.5f;

    /// <summary>
    /// Travel-time multiplier applied for sufficiently skilled pilots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SkillTravelMultiplier = 0.5f;

    /// <summary>
    /// Recharge-time multiplier applied for sufficiently skilled pilots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SkillRechargeMultiplier = 0.75f;

    /// <summary>
    /// Whether this shuttle may be taken through normal hijack mechanics.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Hijackable = true;

    /// <summary>
    /// Whether this console can control a shuttle remotely instead of from onboard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoteControl = false;

    /// <summary>
    /// Duration after forced shutdown before this console can launch again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Round time until which this console remains locked out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LockedOutUntil = TimeSpan.Zero;

    /// <summary>
    /// Whether the launch alarm is currently active for this console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LaunchAlarmStatus;

    /// <summary>
    /// Whether this console may only route to planetside destinations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PlanetOnly;

    // When enabled, destination validation requires a restricted landing policy on the destination.
    /// <summary>
    /// Whether launches require destinations with explicit restricted routing metadata.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresRestrictedDestination;

    // Defines which landing class this shuttle may use.
    /// <summary>
    /// Docking profile used to match this shuttle against destination landing classes.
    /// </summary>
    [DataField]
    public RMCShuttleDockingClass ShuttleDockingClass = RMCShuttleDockingClass.Standard;

    // Optional explicit footprint used instead of the grid AABB for destination fit validation.
    /// <summary>
    /// Optional shuttle footprint override used when validating fit against a destination dock.
    /// </summary>
    [DataField]
    public Box2? DockingBounds;

    // Additional tag filters applied once restricted destination routing is enabled.
    /// <summary>
    /// Landing tags that a restricted destination must include for this shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> AllowedLandingTags = [];

    /// <summary>
    /// Landing tags that make a restricted destination invalid for this shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> DeniedLandingTags = [];

    // Optional server-controlled player routing lock. System launches may still route this shuttle directly.
    /// <summary>
    /// Whether player-facing routing is locked to a single server-selected destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PlayerDestinationLockEnabled;

    /// <summary>
    /// Destination entity players are allowed to select while the routing lock is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? PlayerAllowedDestination;

    /// <summary>
    /// Sound played when launch is forcibly shut down.
    /// </summary>
    [DataField]
    public SoundSpecifier? LaunchAlarmForcedShutdownSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/metalhit.ogg");
}
