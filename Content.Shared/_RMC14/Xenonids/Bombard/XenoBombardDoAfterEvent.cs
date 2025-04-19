using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Bombard;

[Serializable, NetSerializable]
public sealed partial class XenoBombardDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public MapCoordinates Coordinates;
}
