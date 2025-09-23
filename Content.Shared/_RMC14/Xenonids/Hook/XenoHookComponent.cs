using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hook;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoHookComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId TailProto = "RMCOppressorTail";

    [DataField]
    public List<EntityUid> Hooked = new();
}
