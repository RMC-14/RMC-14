using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CMDoorSystem))]
public sealed partial class RMCDoorButtonComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastUse;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public string OffState = "doorctrl";

    [DataField, AutoNetworkedField]
    public string OnState = "doorctrl1";
}
