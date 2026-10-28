using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCPlantTraitFlowersComponent : RMCPlantTraitComponent
{
    [DataField, AutoNetworkedField]
    public string? Icon;

    [DataField, AutoNetworkedField]
    public Color? Color;
}
