using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCConstructionSystem))]
public sealed partial class RMCReplaceOnHijackLandComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Id;
}
