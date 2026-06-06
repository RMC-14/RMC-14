namespace Content.Shared._RMC14.Requisitions;

public sealed class RequisitionsBuyEvent : EntityEventArgs
{
    public readonly EntityUid Buyer;
    public readonly RequisitionsEntry? Order;

    public RequisitionsBuyEvent(EntityUid buyer, RequisitionsEntry? order)
    {
        Buyer = buyer;
        Order = order;
    }
}
