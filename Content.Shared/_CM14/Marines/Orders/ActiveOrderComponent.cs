using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveOrderComponent : Component, IOrderComponent
{
    [DataField]
    public Orders Order;

    /// <summary>
    /// The duration of an order.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration { get; set; }

    public override bool SessionSpecific => true;
}
