using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParatoxinOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int StacksToApply = 5;

    [DataField, AutoNetworkedField]
    public bool ShowPopup = false;
}
