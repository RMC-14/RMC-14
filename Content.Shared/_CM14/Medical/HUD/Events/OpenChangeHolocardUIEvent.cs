
namespace Content.Shared._CM14.Medical.Events;

/// <summary>
///     Indicates to the server to change open a Holocard Change Bound UI for a particular user on a particular target
/// </summary>
public sealed class OpenChangeHolocardUIEvent : BoundUserInterfaceMessage
{
    public NetEntity Owner;
    public NetEntity Target;
    public OpenChangeHolocardUIEvent(NetEntity owner, NetEntity target)
    {
        Owner = owner;
        Target = target;
    }
}
