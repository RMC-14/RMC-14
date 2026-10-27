using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class InnateCommandSpeechComponent : Component
{
    [DataField]
    public EntProtoId CommandSpeechActionId = "ActionCommandSpeechToggle";

    public EntityUid? CommandSpeechAction;

    [DataField, AutoNetworkedField]
    public bool Active;
}
