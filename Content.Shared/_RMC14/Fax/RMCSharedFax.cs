using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Fax;

/// <summary>
/// RMC-specific fax message for copying multiple papers at once.
/// This extends the base fax functionality with RMC-specific behavior.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCFaxCopyMultipleMessage : BoundUserInterfaceMessage
{
    public int Copies { get; }

    public RMCFaxCopyMultipleMessage(int copies)
    {
        Copies = copies;
    }
}
