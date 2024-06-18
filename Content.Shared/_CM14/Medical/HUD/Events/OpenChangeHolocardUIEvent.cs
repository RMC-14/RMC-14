
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.HUD.Events;

/// <summary>
///     Indicates to the server to change open a Holocard Change Bound UI for a particular user on a particular target
/// </summary>
[NetSerializable, Serializable]
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
