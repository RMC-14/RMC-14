using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCRoundInfoComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OperationName = string.Empty;

    [DataField, AutoNetworkedField]
    public string PlanetName = string.Empty;

    [DataField, AutoNetworkedField]
    public string ShipName = string.Empty;
}
