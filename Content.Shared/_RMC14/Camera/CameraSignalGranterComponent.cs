using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraSignalGranterComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> ProtoIds = new();
}
