using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPlantTraitBioluminescentComponent : RMCPlantTraitComponent
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
