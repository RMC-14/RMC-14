using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(SharedXenoConstructionSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerDesignNodeVariantComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Wall;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Door;
}
