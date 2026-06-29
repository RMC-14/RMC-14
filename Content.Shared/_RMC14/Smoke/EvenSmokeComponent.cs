using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Smoke;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCSmokeSystem))]
public sealed partial class EvenSmokeComponent : Component
{
    [DataField]
    public int Range = 2;

    /// <summary>
    ///     The amount of times it should instantly spread after spawning, this value will be subtracted from the <see cref="Range"/> after the initial spread is done.
    ///     Make sure the <see cref="Spawn"/> prototype does NOT have the <see cref="EvenSmokeComponent"/> if you set this higher than 0.
    /// </summary>
    /// <example>
    /// Range = 3 and InitialSpread = 2: It will instantly spread twice, then spreads one more time after the normal delay.
    /// </example>
    [DataField]
    public int InitialSpread;

    /// <summary>
    /// If true, any damage over time effects of this smoke get multiplied by its Range.
    /// In effect, the closer one is to the center of the smoke, the more damage they take,
    /// while they take less damage at the edges.
    /// Specifically, only the regular, non-armor piercing damage gets multiplied.
    /// </summary>
    [DataField]
    public bool RangeMultipliesRegularDamage = false;
}
