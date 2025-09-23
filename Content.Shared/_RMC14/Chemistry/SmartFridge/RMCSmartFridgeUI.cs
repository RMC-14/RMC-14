using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

[Serializable, NetSerializable]
public enum RMCSmartFridgeUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCSmartFridgeVendMsg(NetEntity vend) : BoundUserInterfaceMessage
{
    public readonly NetEntity Vend = vend;
}
