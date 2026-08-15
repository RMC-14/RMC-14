using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.TacticalMap;

[ByRefEvent]
public readonly record struct TacticalMapXenoRevealRangeEvent
{
    public readonly List<TacticalMapXenoRevealSource> Sources = new();

    public TacticalMapXenoRevealRangeEvent()
    {
    }
}

public readonly record struct TacticalMapXenoRevealSource(EntityUid Grid, Vector2i Indices, float Range);
