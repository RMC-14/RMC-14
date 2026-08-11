// ReSharper disable once CheckNamespace
namespace Content.Shared.Explosion.Components;

/// <summary>
///     Extends the upstream explosive component with RMC14-specific behavior.
/// </summary>
public sealed partial class ExplosiveComponent
{
    /// <summary>
    ///     Damage multiplier applied to downed entities. Defaults to no adjustment.
    /// </summary>
    [DataField]
    public float ProneDamageMultiplier = 1f;
}
