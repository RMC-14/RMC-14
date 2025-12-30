using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFireColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.Orange;
}
