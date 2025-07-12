using Robust.Shared.GameStates;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class ActiveParaDropComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? DropTarget;
}
