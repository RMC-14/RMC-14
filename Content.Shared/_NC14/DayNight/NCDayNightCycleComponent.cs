using Robust.Shared.GameStates;

namespace Content.Shared._NC14.DayNight;

/// <summary>
/// Marks a map to run a deterministic day/night light cycle driven by the round seed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCDayNightCycleComponent : Component
{
    /// <summary>
    /// Baseline duration of a full day/night cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BaseDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Jitter applied to the duration (positive or negative).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DurationJitter = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether this cycle runs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
