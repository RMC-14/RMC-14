using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[Serializable, NetSerializable]
public sealed class OverwatchInsubordinateMarineSelectedEvent: EntityEventArgs
{
    public NetEntity Actor { get; }
    public NetEntity Marine { get; }

    public OverwatchInsubordinateMarineSelectedEvent(NetEntity actor, NetEntity marine)
    {
        Actor = actor;
        Marine = marine;
    }
}
