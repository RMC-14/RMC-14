using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Piping.Binary.Components;

[RegisterComponent, Access(typeof(SignalControlledValveSystem))]
public sealed partial class SignalControlledValveComponent : Component
{
    [DataField("openPort")]
    public ProtoId<SinkPortPrototype> OpenPort = "Open";

    [DataField("closePort")]
    public ProtoId<SinkPortPrototype> ClosePort = "Close";

    [DataField("togglePort")]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
}
