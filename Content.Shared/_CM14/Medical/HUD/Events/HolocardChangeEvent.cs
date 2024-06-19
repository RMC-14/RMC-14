
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.HUD.Events;

/// <summary>
///     Indicates to the server to change the holocard status of a entity
/// </summary>
[NetSerializable, Serializable]
public sealed class HolocardChangeEvent : BoundUserInterfaceMessage
{
    public HolocardStaus NewHolocardStatus;

    /// <summary>
    /// The entity changing the holocard
    /// </summary>
    public NetEntity Owner;

    public HolocardChangeEvent(NetEntity owner, HolocardStaus newHolocardStatus)
    {
        Owner = owner;
        NewHolocardStatus = newHolocardStatus;
    }
}
