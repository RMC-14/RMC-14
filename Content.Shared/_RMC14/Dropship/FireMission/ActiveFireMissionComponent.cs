using System.Numerics;
using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.FireMission;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveFireMissionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FireMissionData? FireMissionData;

    /// <summary>
    ///     The start time of the firemission, this is the moment the pilot presses the "fire" button.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>
    ///     How long it takes before the warnings appear to entities near the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The duration of the "warning" phase.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WarningDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The time between each step of the fire mission.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StepDelay = TimeSpan.FromSeconds(0.25);

    /// <summary>
    ///     The current step of the fire mission.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStep = 1;

    /// <summary>
    ///     The step at which the mission ends.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxSteps;

    /// <summary>
    ///     The coordinates of the initial target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates TargetCoordinates;

    /// <summary>
    ///     The direction the mission will travel towards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction StrikeVector;

    /// <summary>
    ///     The offset from the initial target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    /// <summary>
    ///     The warnings to display during the "warning phase".
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<FireMissionWarning> Warnings = new()
    {
        new FireMissionWarning(TimeSpan.FromSeconds(1), "rmc-dropship-firemission-warning-early", 15),
        new FireMissionWarning(TimeSpan.FromSeconds(3), "rmc-dropship-firemission-warning", 15),
        new FireMissionWarning(TimeSpan.FromSeconds(4), "rmc-dropship-firemission-warning", 10),
    };

    /// <summary>
    ///     Has the missions duration passed the startDelay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FireMissionPhase CurrentPhase = FireMissionPhase.Preparing;

    /// <summary>
    ///     The sound to play at the start location of the fire mission.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/dropship_sonic_boom.ogg", AudioParams.Default.WithVolume(4).WithRolloffFactor(8).WithMaxDistance(50));

    /// <summary>
    ///     The time between starting a fire mission and being able to start a new one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MissionCooldown = TimeSpan.FromSeconds(25);

    [DataField, AutoNetworkedField]
    public EntityUid? MissionEye;

    [DataField, AutoNetworkedField]
    public EntityUid? WatchingTerminal;
}

[Serializable, NetSerializable]
public sealed class FireMissionWarning(TimeSpan offset, string message, int messageRange, bool messageSent = false)
{
    public TimeSpan Offset = offset;
    public string Message = message;
    public int MessageRange = messageRange;
    public bool MessageSent = messageSent;
}

[Serializable, NetSerializable]
public enum FireMissionPhase
{
    Preparing,
    Approaching,
    Firing,
    Cooldown,
}
