using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntelRandomSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SelectedVariant = string.Empty;
}
