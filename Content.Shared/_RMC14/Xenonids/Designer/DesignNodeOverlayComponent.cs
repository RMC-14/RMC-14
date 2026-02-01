using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(DesignerNodeOverlaySystem))]

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class DesignNodeOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Overlay;
}
