using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCExtinguisherPowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Power = 7;
}
