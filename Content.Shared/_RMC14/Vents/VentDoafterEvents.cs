using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vents;

[Serializable, NetSerializable]
public sealed partial class VentEnterDoafterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VentExitDoafterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public Direction ExitDirection;

    public VentExitDoafterEvent(Direction direction)
    {
        ExitDirection = direction;
    }
}
