using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Marines.Orders;

public partial interface IOrderComponent : IComponent
{
    List<(FixedPoint2 Multiplier, TimeSpan ExpiresAt)> Received { get; }
}
