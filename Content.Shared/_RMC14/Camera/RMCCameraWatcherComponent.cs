using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCCameraSystem))]
public sealed partial class RMCCameraWatcherComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Computer;

    [DataField, AutoNetworkedField]
    public HashSet<NetEntity> Overrides = new();
}
