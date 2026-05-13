using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(SharedXenoConstructionSystem))]
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class DesignerDeleteDesignNodeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int PlasmaCost = 25;

    [DataField, AutoNetworkedField]
    public bool OnlyOwnNodes = true;
}
