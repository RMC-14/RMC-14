using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.StatusEffect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStatusEffectSystem))]
public sealed partial class RMCStunResistanceComponent : Component
{
    /// <summary>
    /// The final stun duration (after endurance skill) is divided by this number.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Resistance = 1.5f;
}
