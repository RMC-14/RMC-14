using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage.TimedInvincibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCTimedInvincibilitySystem))]
public sealed partial class RMCTimedInvincibilityComponent : Component
{
    /// <summary>
    /// How long the entity will be invincible for in seconds
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Lifetime = 1f;
}
