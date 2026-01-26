using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch
{
    [NetSerializable, Serializable]
    public sealed class OverwatchCameraAdjustOffsetEvent : EntityEventArgs
    {
        public NetEntity Actor { get; }
        public OverwatchDirection Direction { get; }

        public OverwatchCameraAdjustOffsetEvent(NetEntity actor, OverwatchDirection direction)
        {
            Actor = actor;
            Direction = direction;
        }
    }
}
