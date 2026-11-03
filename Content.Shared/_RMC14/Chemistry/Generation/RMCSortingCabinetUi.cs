using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public enum RMCSortingCabinetUi : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum RMCSortingCabinetSort : byte
{
    None,
    Value,
    Overdose,
    Class,
}

[Serializable, NetSerializable]
public sealed class RMCSortingCabinetTakeOutBuiMsg : BoundUserInterfaceMessage
{
    public NetEntity Document;
    public RMCSortingCabinetTakeOutBuiMsg(NetEntity document) => Document = document;
}

[Serializable, NetSerializable]
public sealed class RMCSortingCabinetSetSortBuiMsg : BoundUserInterfaceMessage
{
    public RMCSortingCabinetSort Sort;
    public RMCSortingCabinetSetSortBuiMsg(RMCSortingCabinetSort sort) => Sort = sort;
}

[Serializable, NetSerializable]
public sealed class RMCSortingCabinetToggleOrderBuiMsg : BoundUserInterfaceMessage;
