using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Forge.DayNight;

/// <summary>
/// Marks a map to run a deterministic day/night light cycle driven by the round seed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFDayNightCycleComponent : Component
{
    /// <summary>
    /// Baseline duration of a full day/night cycle.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan BaseDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Jitter applied to the duration (positive or negative).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationJitter = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether this cycle runs.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled = true;

    /// <summary>
    /// Current simulated hour (0-23) on this map.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public int Hour;

    /// <summary>
    /// Current simulated minute (0-59) on this map.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public int Minute;

    /// <summary>
    /// Current simulated day number (starts at 1).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public long DayNumber = 1;

    /// <summary>
    /// Current phase of the day.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public CFDayPhase Phase = CFDayPhase.Day;

    /// <summary>
    /// Normalized position in the cycle [0,1).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public float NormalizedTime;

    /// <summary>
    /// Current map temperature in Kelvin (null if no controller).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public float? TemperatureKelvin;
}
