using Content.Shared._RMC14.Requisitions.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

[Serializable, NetSerializable]
public enum RequisitionsUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RequisitionsBuiState : BoundUserInterfaceState
{
    public RequisitionsElevatorMode? PlatformLowered;
    public bool Busy;
    public int Balance;
    public bool Full;
    public int OrderCount;
    public int Capacity;
    public bool BlackMarketUnlocked;
    public int BlackMarketBalance;
    public RequisitionsBlackMarketStatus BlackMarketStatus;
    public List<RequisitionsPendingOrder> PendingOrders;

    public RequisitionsBuiState(
        RequisitionsElevatorMode? platformLowered,
        bool busy,
        int balance,
        bool full,
        int orderCount,
        int capacity,
        bool blackMarketUnlocked,
        int blackMarketBalance,
        RequisitionsBlackMarketStatus blackMarketStatus,
        List<RequisitionsPendingOrder> pendingOrders)
    {
        PlatformLowered = platformLowered;
        Busy = busy;
        Balance = balance;
        Full = full;
        OrderCount = orderCount;
        Capacity = capacity;
        BlackMarketUnlocked = blackMarketUnlocked;
        BlackMarketBalance = blackMarketBalance;
        BlackMarketStatus = blackMarketStatus;
        PendingOrders = pendingOrders;
    }
}

[Serializable, NetSerializable]
public sealed class RequisitionsBuyMsg(int category, int order) : BoundUserInterfaceMessage
{
    public int Category = category;
    public int Order = order;
}

[Serializable, NetSerializable]
public sealed class RequisitionsCartItem(int category, int order, int amount)
{
    public int Category = category;
    public int Order = order;
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RequisitionsBuyCartMsg(List<RequisitionsCartItem> items) : BoundUserInterfaceMessage
{
    public List<RequisitionsCartItem> Items = items;
}

[Serializable, NetSerializable]
public sealed class RequisitionsBuyBlackMarketCartMsg(List<RequisitionsCartItem> items) : BoundUserInterfaceMessage
{
    public List<RequisitionsCartItem> Items = items;
}

[Serializable, NetSerializable]
public sealed class RequisitionsPendingOrder(RequisitionsEntry entry, int amount)
{
    public RequisitionsEntry Entry = entry;
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RequisitionsPlatformMsg(bool raise) : BoundUserInterfaceMessage
{
    public bool Raise = raise;
}
