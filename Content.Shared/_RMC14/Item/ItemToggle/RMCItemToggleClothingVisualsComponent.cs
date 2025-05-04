using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Item.ItemToggle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCItemToggleClothingVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Prefix = "on";
}
