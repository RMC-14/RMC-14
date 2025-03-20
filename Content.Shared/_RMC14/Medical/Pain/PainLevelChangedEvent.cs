namespace Content.Shared._RMC14.Medical.Pain;

/// <summary>
///     Raised when level of pain changed.
/// </summary>

public sealed class PainLevelChangedEvent : EntityEventArgs
{
    public readonly int Level;

    public PainLevelChangedEvent(int level)
    {
        Level = level;
    }
}
