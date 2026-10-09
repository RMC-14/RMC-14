using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Input;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCKeybindActionsComponent : Component
{
    /// <summary>
    /// Action prototypes accepted by each semantic keybind, in priority order.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<RMCKeybindActionSlot, List<EntProtoId>> Actions = new();
}
