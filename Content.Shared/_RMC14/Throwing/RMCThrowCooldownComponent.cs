using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Throwing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCThrowCooldownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public int ThrowLimit = 2;

    [DataField, AutoNetworkedField]
    public int ItemsThrown;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEndsAt;
}
