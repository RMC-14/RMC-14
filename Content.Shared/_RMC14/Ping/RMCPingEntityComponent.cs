using System;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Ping;

public interface RMCPingEntityComponent
{
    string PingType { get; set; }
    EntityUid Creator { get; set; }
    TimeSpan Lifetime { get; set; }
    TimeSpan DeleteAt { get; set; }
    EntityUid? AttachedTarget { get; set; }
    EntityCoordinates? LastKnownCoordinates { get; set; }
    Vector2 WorldPosition { get; set; }
    bool ShowWaypoint { get; set; }
    Vector2 AttachedOffset { get; set; }
}
