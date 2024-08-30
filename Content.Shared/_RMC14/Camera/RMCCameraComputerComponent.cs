using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCCameraSystem))]
public sealed partial class RMCCameraComputerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Id;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentCamera;

    [DataField, AutoNetworkedField]
    public List<NetEntity> CameraIds = new();

    [DataField, AutoNetworkedField]
    public List<string> CameraNames = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Watchers = new();

    [DataField, AutoNetworkedField]
    public LocId? Title;
}
