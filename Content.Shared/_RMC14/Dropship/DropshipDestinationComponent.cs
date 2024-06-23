using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipDestinationComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Ship;

    [DataField, AutoNetworkedField]
    public bool AutoRecall;
}
