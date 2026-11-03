using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public enum RMCSortingCabinetUi : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCSortingCabinetTakeOutBuiMsg : BoundUserInterfaceMessage
{
    public NetEntity Document;
    public RMCSortingCabinetTakeOutBuiMsg(NetEntity document) => Document = document;
}
