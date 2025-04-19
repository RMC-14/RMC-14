using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCCameraSystem))]
public sealed partial class RMCCameraComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Id;

    [DataField, AutoNetworkedField]
    public bool Rename = true;
}
