using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

[Serializable, NetSerializable]
public sealed class RequisitionsRequest(
    int number,
    string requestedBy,
    string rank,
    string reason,
    List<RequisitionsPendingOrder> items,
    int totalCost)
{
    public int Number = number;
    public string RequestedBy = requestedBy;
    public string Rank = rank;
    public string Reason = reason;
    public List<RequisitionsPendingOrder> Items = items;
    public int TotalCost = totalCost;
    public int Revision;
    public bool Closed;
}

[Serializable, NetSerializable]
public sealed class RequisitionsOrderDetails(int number, string requestedBy, string approvedBy, NetEntity account)
{
    public int Number = number;
    public string RequestedBy = requestedBy;
    public string ApprovedBy = approvedBy;
    public NetEntity Account = account;
}

[Serializable, NetSerializable]
public sealed class RequisitionsSubmitRequestMsg(List<RequisitionsCartItem> items, string reason)
    : BoundUserInterfaceMessage
{
    public readonly List<RequisitionsCartItem> Items = items;
    public readonly string Reason = reason;
}

[Serializable, NetSerializable]
public sealed class RequisitionsRespondRequestMsg(int number, bool approve) : BoundUserInterfaceMessage
{
    public readonly int Number = number;
    public readonly bool Approve = approve;
    public int Revision;
    public List<RequisitionsRequestSelection>? Selections;
    public int? ExpectedCost;
}

[Serializable, NetSerializable]
public sealed class RequisitionsRequestSelection(int line, int amount, bool purchase)
{
    public int Line = line;
    public int Amount = amount;
    public bool Purchase = purchase;
}

[Serializable, NetSerializable]
public sealed class RequisitionsDeliveryLine(int line, int amount)
{
    public int Line = line;
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RequisitionsEjectRequestMsg(int number) : BoundUserInterfaceMessage
{
    public readonly int Number = number;
}

[Serializable, NetSerializable]
public sealed class RequisitionsRequestResultMsg(bool success, string message) : BoundUserInterfaceMessage
{
    public readonly bool Success = success;
    public readonly string Message = message;
}
