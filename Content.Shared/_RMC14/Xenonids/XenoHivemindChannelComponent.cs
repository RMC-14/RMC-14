using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoHivemindChannelComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> Channel = SharedChatSystem.HivemindChannel;
}
