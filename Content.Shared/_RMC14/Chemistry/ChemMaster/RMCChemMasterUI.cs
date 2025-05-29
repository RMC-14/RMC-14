using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[Serializable, NetSerializable]
public enum RMCChemMasterUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleLabelMsg(string label) : BoundUserInterfaceMessage
{
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleColorMsg(RMCPillBottleColors color) : BoundUserInterfaceMessage
{
    public readonly RMCPillBottleColors Color = color;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleFillMsg(NetEntity bottle, bool fill) : BoundUserInterfaceMessage
{
    public readonly NetEntity Bottle = bottle;
    public readonly bool Fill = fill;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleTransferMsg(NetEntity bottle) : BoundUserInterfaceMessage
{
    public readonly NetEntity Bottle = bottle;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleEjectMsg(NetEntity bottle) : BoundUserInterfaceMessage
{
    public readonly NetEntity Bottle = bottle;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterBeakerEjectMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterBeakerTransferMsg(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterBeakerTransferAllMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterBeakerTransferAllReagentsMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterBufferModeMsg(RMCChemMasterBufferMode mode) : BoundUserInterfaceMessage
{
    public readonly RMCChemMasterBufferMode Mode = mode;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterBufferTransferMsg(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterBufferTransferAllMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterSetPillAmountMsg(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterSetPillTypeMsg(int type) : BoundUserInterfaceMessage
{
    public readonly int Type = type;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterCreatePillsMsg : BoundUserInterfaceMessage;
