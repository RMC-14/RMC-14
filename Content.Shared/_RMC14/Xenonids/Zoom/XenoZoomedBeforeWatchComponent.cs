using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Zoom;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoZoomedBeforeWatchComponent : Component
{
    [DataField]
    public bool WasZoomed;
}