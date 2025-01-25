using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public bool CheckStructureSelected;

    [DataField(required: true), AutoNetworkedField]
    public bool CheckWeeds;

    [DataField, AutoNetworkedField]
    public bool CanUpgrade;
}
