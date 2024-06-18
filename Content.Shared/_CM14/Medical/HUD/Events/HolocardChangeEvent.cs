
namespace Content.Shared._CM14.Medical.Events;

/// <summary>
///     Indicates to the server to change the holocard status of a entity
/// </summary>
public sealed class HolocardChangeEvent : BoundUserInterfaceMessage
{
    public HolocardStaus NewHolocardStatus;
    public HolocardChangeEvent(HolocardStaus newHolocardStatus)
    {
        NewHolocardStatus = newHolocardStatus;
    }
}
