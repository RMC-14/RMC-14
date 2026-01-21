using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

/// <summary>
/// Tracks the nutriment overdose state.
/// When present, applies slowdown when overdosing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NutrimentOverdoseComponent : Component
{
    /// <summary>
    /// The collective volume of nutriment after removal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RemainingVolume;

    /// <summary>
    /// How long the slowdown lasts when overdosing.
    /// CMSS13 uses Superslow(20) which is 20 * 0.1 = 2 seconds.
    /// </summary>
    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(2);
}
