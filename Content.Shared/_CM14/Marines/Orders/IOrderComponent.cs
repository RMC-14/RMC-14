using Content.Shared.FixedPoint;

namespace Content.Shared._CM14.Marines.Orders;

public partial interface IOrderComponent : IComponent
{
    List<(FixedPoint2 Multiplier, TimeSpan ExpiresAt)> Received { get; }
}
