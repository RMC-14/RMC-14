using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class ActiveParaDropComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? DropTarget;
}
