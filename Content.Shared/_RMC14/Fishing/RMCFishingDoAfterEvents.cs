using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Fishing;

[Serializable, NetSerializable]
public sealed partial class RMCFishingDeployDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public Direction Direction;

    public RMCFishingDeployDoAfterEvent(Direction direction)
    {
        Direction = direction;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCFishingPackDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCFishingWaitDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public int Token;

    public RMCFishingWaitDoAfterEvent(int token)
    {
        Token = token;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCFishingSpearDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Coordinates;

    public RMCFishingSpearDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
