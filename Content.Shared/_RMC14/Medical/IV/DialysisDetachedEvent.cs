using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.IV;
/// <summary>
/// Raised when a portable dialysis machine detachment state changes.
/// Sent from server to client to trigger immediate sprite updates.
/// </summary>
[Serializable, NetSerializable]
public sealed class DialysisDetachedEvent(NetEntity dialysis, bool isDetaching) : EntityEventArgs
{
    public NetEntity Dialysis = dialysis;
    public bool IsDetaching = isDetaching;
}
