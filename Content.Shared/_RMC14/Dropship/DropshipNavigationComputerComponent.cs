using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

using Content.Shared._RMC14.ERT;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
/// <summary>
/// Navigation settings for a dropship flight computer, including ERT-specific routing flags when used on response shuttles.
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

    // When enabled, destination validation requires the berth to be marked as an ERT landing zone.
    [DataField, AutoNetworkedField]
    public bool RequiresERTLandingZone;

    // Defines which class of emergency berth this shuttle may use.
    [DataField]
    public RMCERTShuttleDockingClass ERTDockingClass = RMCERTShuttleDockingClass.Standard;

    // Optional explicit footprint used instead of the grid AABB for berth fit validation.
    [DataField]
    public Box2? DockingBounds;

    // Additional tag filters applied once ERT-only berth routing is enabled.
    [DataField, AutoNetworkedField]
    public List<string> AllowedERTLandingTags = [];

    [DataField, AutoNetworkedField]
    public List<string> DeniedERTLandingTags = [];

    [DataField]
    public SoundSpecifier? LaunchAlarmForcedShutdownSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/metalhit.ogg");
}
