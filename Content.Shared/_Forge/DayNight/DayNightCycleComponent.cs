using Robust.Shared.GameStates;

namespace Content.Shared._Forge.DayNight;

/// <summary>
/// Marks a map to run a deterministic day/night light cycle driven by the round seed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DayNightCycleComponent : Component
{
    /// <summary>
    /// Baseline duration of a full day/night cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BaseDuration = TimeSpan.FromHours(1);

    /// <summary>
    /// Jitter applied to the duration (positive or negative).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DurationJitter = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether this cycle runs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Current simulated hour (0-23) on this map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Hour;

    /// <summary>
    /// Current simulated minute (0-59) on this map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Minute;

    /// <summary>
    /// Current simulated day number (starts at 1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public long DayNumber = 1;

    /// <summary>
    /// Current phase of the day.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DayPhase Phase = DayPhase.Day;

    /// <summary>
    /// Normalized position in the cycle [0,1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NormalizedTime;
}
