using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class DoorSignalControlComponent : Component
    {
        [DataField("openPort")]
        public ProtoId<SinkPortPrototype> OpenPort = "Open";

        [DataField("closePort")]
        public ProtoId<SinkPortPrototype> ClosePort = "Close";

        [DataField("togglePort")]
        public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

        [DataField("boltPort")]
        public ProtoId<SinkPortPrototype> InBolt = "DoorBolt";

        [DataField("onOpenPort")]
        public ProtoId<SourcePortPrototype> OutOpen = "DoorStatus";
    }
}
