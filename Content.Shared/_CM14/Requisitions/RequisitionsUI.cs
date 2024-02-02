using Content.Shared._CM14.Requisitions.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Requisitions;

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

    public RequisitionsBuiState(RequisitionsElevatorMode? platformLowered, bool busy, int balance, bool full)
    {
        PlatformLowered = platformLowered;
        Busy = busy;
        Balance = balance;
        Full = full;
    }
}

[Serializable, NetSerializable]
public sealed class RequisitionsBuyMsg : BoundUserInterfaceMessage
{
    public int Category;
    public int Order;

    public RequisitionsBuyMsg(int category, int order)
    {
        Category = category;
        Order = order;
    }
}

[Serializable, NetSerializable]
public sealed class RequisitionsPlatformMsg : BoundUserInterfaceMessage
{
    public bool Raise;

    public RequisitionsPlatformMsg(bool raise)
    {
        Raise = raise;
    }
}
