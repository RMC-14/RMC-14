using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UserPointblankCooldownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan LastPBAt;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenPBs = TimeSpan.FromSeconds(1.1);
}
