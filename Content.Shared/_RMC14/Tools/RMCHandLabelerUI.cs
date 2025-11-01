using Content.Shared._RMC14.Chemistry.ChemMaster;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tools;

[Serializable, NetSerializable]
public enum RMCHandLabelerUiKey
{
    PillBottleColor
}

[Serializable, NetSerializable]
public sealed class RMCHandLabelerPillBottleColorMsg(NetEntity pillBottle, RMCPillBottleColors color) : BoundUserInterfaceMessage
{
    public NetEntity PillBottle = pillBottle;
    public RMCPillBottleColors Color = color;
}
