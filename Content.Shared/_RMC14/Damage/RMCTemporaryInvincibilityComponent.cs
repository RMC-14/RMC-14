using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCTemporaryInvincibilityComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
