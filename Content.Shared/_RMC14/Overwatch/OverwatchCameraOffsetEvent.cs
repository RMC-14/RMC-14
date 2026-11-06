using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch
{
    /// <summary>
    /// Raised by camera offset keybinds to sync zoom and offset with the server
    /// </summary>
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
