using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rules;

[Serializable, NetSerializable]
public sealed class RMCScuttleCinematicEvent(TimeSpan duration) : EntityEventArgs
{
    public readonly TimeSpan Duration = duration;
}
