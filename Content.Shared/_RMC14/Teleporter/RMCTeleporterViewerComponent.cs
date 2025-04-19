using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Teleporter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTeleporterSystem))]
public sealed partial class RMCTeleporterViewerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Id = string.Empty;
}
