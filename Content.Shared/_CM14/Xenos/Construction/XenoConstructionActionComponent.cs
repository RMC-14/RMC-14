using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public bool CheckStructureSelected;

    [DataField(required: true), AutoNetworkedField]
    public bool CheckWeeds;
}
