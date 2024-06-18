
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.HUD.Events;

/// <summary>
///     Indicates to the server to change the holocard status of a entity
/// </summary>
[NetSerializable, Serializable]
public sealed class HolocardChangeEvent : BoundUserInterfaceMessage
{
    public HolocardStaus NewHolocardStatus;
    public HolocardChangeEvent(HolocardStaus newHolocardStatus)
    {
        NewHolocardStatus = newHolocardStatus;
    }
}
