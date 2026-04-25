using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// High-level berth profile used by restricted RMC shuttle routing.
/// </summary>
public enum RMCShuttleDockingClass
{
    Standard,
    Small,
    Big,
}

/// <summary>
/// Maps an abstract shuttle profile to concrete berth class tags.
/// </summary>
public static class RMCShuttleDocking
{
    private static readonly string[] SmallDockClasses = ["internal", "external_side"];
    private static readonly string[] StandardDockClasses = ["internal"];
    private static readonly string[] BigDockClasses = ["external_hangar"];

    /// <summary>
    /// Returns the berth classes the shuttle may use for automatic routing and nav-console validation.
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
/// Navigation settings for a dropship flight computer, including restricted berth routing flags.
/// </summary>
public sealed partial class DropshipNavigationComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPilot";

    [DataField, AutoNetworkedField]
    public int MultiplierSkillLevel = 2;

    [DataField, AutoNetworkedField]
    public int FlyBySkillLevel = 2;

    [DataField, AutoNetworkedField]
    public float SkillFlyByMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public float SkillTravelMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float SkillRechargeMultiplier = 0.75f;

    [DataField, AutoNetworkedField]
    public bool Hijackable = true;

    [DataField, AutoNetworkedField]
    public bool RemoteControl = false;

    [DataField, AutoNetworkedField]
    public TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan LockedOutUntil = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool LaunchAlarmStatus;

    [DataField, AutoNetworkedField]
    public bool PlanetOnly;

    // When enabled, destination validation requires the destination to be marked as a restricted shuttle berth.
    [DataField, AutoNetworkedField]
    public bool RequiresShuttleBerth;

    // Defines which berth class this shuttle may use.
    [DataField]
    public RMCShuttleDockingClass ShuttleDockingClass = RMCShuttleDockingClass.Standard;

    // Optional explicit footprint used instead of the grid AABB for berth fit validation.
    [DataField]
    public Box2? DockingBounds;

    // Additional tag filters applied once restricted berth routing is enabled.
    [DataField, AutoNetworkedField]
    public List<string> AllowedLandingTags = [];

    [DataField, AutoNetworkedField]
    public List<string> DeniedLandingTags = [];

    [DataField]
    public SoundSpecifier? LaunchAlarmForcedShutdownSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/metalhit.ogg");
}
