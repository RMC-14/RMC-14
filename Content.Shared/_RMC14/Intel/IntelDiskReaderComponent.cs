using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelDiskReaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "intel_disk";

    [DataField, AutoNetworkedField]
    public EntityUid? LastUser;
}
