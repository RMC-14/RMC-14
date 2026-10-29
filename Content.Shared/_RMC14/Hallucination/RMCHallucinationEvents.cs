using System.Numerics;
using Content.Shared.Chat;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Hallucination;

[Serializable, NetSerializable]
public sealed class RMCHallucinationVisualEvent(EntProtoId prototype, string? state, NetCoordinates coordinates, TimeSpan duration) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly string? State = state;
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly TimeSpan Duration = duration;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationItemEvent(EntProtoId prototype, string slot, string? slotGroup, TimeSpan duration) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly string Slot = slot;
    public readonly string? SlotGroup = slotGroup;
    public readonly TimeSpan Duration = duration;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerSpawnEvent(int id, NetEntity clone, NetCoordinates coordinates) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetEntity Clone = clone;
    public readonly NetCoordinates Coordinates = coordinates;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerMoveEvent(int id, NetCoordinates coordinates, Vector2 velocity, Direction direction) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly Vector2 Velocity = velocity;
    public readonly Direction Direction = direction;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerCollapseEvent(int id, TimeSpan fade) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly TimeSpan Fade = fade;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerRemoveEvent(int id) : EntityEventArgs
{
    public readonly int Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerHitEvent(int id, NetEntity weapon) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetEntity Weapon = weapon;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationMovingVisualEvent(
    EntProtoId prototype,
    string? state,
    NetCoordinates from,
    NetCoordinates to,
    TimeSpan delay,
    TimeSpan travel,
    TimeSpan linger,
    float spinSpeed = 0) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly string? State = state;
    public readonly NetCoordinates From = from;
    public readonly NetCoordinates To = to;
    public readonly TimeSpan Delay = delay;
    public readonly TimeSpan Travel = travel;
    public readonly TimeSpan Linger = linger;
    public readonly float SpinSpeed = spinSpeed;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationAttackerSwingEvent(int id, NetEntity? weapon, EntProtoId? animation) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetEntity? Weapon = weapon;
    public readonly EntProtoId? Animation = animation;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationBodyEvent(int id, NetEntity clone, NetCoordinates coordinates, TimeSpan duration) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetEntity Clone = clone;
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly TimeSpan Duration = duration;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationSpeechEvent(int id, bool body, ChatChannel channel, string message, string wrappedMessage) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly bool Body = body;
    public readonly ChatChannel Channel = channel;
    public readonly string Message = message;
    public readonly string WrappedMessage = wrappedMessage;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationFaceEvent(EntProtoId prototype, TimeSpan duration, TimeSpan fade) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly TimeSpan Duration = duration;
    public readonly TimeSpan Fade = fade;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationDisguiseEvent(NetEntity target, EntProtoId xeno, TimeSpan duration) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly EntProtoId Xeno = xeno;
    public readonly TimeSpan Duration = duration;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationLightsEvent(
    TimeSpan duration,
    float range,
    TimeSpan onMin,
    TimeSpan onMax,
    TimeSpan offMin,
    TimeSpan offMax,
    float brownoutDepth) : EntityEventArgs
{
    public readonly TimeSpan Duration = duration;
    public readonly float Range = range;
    public readonly TimeSpan OnMin = onMin;
    public readonly TimeSpan OnMax = onMax;
    public readonly TimeSpan OffMin = offMin;
    public readonly TimeSpan OffMax = offMax;
    public readonly float BrownoutDepth = brownoutDepth;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationLurkerEvent(
    EntProtoId prototype,
    NetCoordinates coordinates,
    TimeSpan duration,
    float vanishRange,
    float hoverRange,
    TimeSpan fade) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly TimeSpan Duration = duration;
    public readonly float VanishRange = vanishRange;
    public readonly float HoverRange = hoverRange;
    public readonly TimeSpan Fade = fade;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationWeedsEvent(
    EntProtoId weedsPrototype,
    NetEntity grid,
    Vector2i node,
    List<Vector2i> tiles,
    List<int> steps,
    TimeSpan spreadDelay,
    TimeSpan fadeAt,
    TimeSpan fade) : EntityEventArgs
{
    public readonly EntProtoId WeedsPrototype = weedsPrototype;
    public readonly NetEntity Grid = grid;
    public readonly Vector2i Node = node;
    public readonly List<Vector2i> Tiles = tiles;
    public readonly List<int> Steps = steps;
    public readonly TimeSpan SpreadDelay = spreadDelay;
    public readonly TimeSpan FadeAt = fadeAt;
    public readonly TimeSpan Fade = fade;
}

[Serializable, NetSerializable]
public sealed class RMCHallucinationParasiteEvent(
    EntProtoId prototype,
    NetCoordinates start,
    float runSpeed,
    float leapRange,
    TimeSpan leapDelay,
    TimeSpan leapTime,
    TimeSpan giveUp) : EntityEventArgs
{
    public readonly EntProtoId Prototype = prototype;
    public readonly NetCoordinates Start = start;
    public readonly float RunSpeed = runSpeed;
    public readonly float LeapRange = leapRange;
    public readonly TimeSpan LeapDelay = leapDelay;
    public readonly TimeSpan LeapTime = leapTime;
    public readonly TimeSpan GiveUp = giveUp;
}
