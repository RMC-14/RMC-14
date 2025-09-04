using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
namespace Content.Shared._RMC14.Slow;

[RegisterComponent, NetworkedComponent]

public sealed partial class RMCSpeciesSlowdownModifierComponent : Component
{
    /// <summary>
    /// The value used in the slow calculations, ported 1:1 from CM13.
    /// </summary>
    [DataField]
    public float SlowModifier;

    /// <summary>
    /// The value used in the superslow calculations, ported 1:1 from CM13.
    /// </summary>
    [DataField]
    public float SuperSlowModifier;

    [DataField]
    public float DurationMultiplier = 1.0f;

    [DataField]
    public string[] StatusesToUpdateOn = { "Stun", "KnockedDown", "Unconscious" };
}
