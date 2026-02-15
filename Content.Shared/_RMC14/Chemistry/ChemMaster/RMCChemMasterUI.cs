using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
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
public sealed class RMCChemMasterBeakerTransferMsg(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype> Reagent = reagent;
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
public sealed class RMCChemMasterBufferTransferMsg(ProtoId<ReagentPrototype> reagent, FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype> Reagent = reagent;
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
public sealed class RMCChemMasterSetPillTypeMsg(uint type) : BoundUserInterfaceMessage
{
    public readonly uint Type = type;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterCreatePillsMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterPillBottleSelectAllMsg(bool selectAll) : BoundUserInterfaceMessage
{
    public readonly bool SelectAll = selectAll;
}

[Serializable, NetSerializable]
public sealed class RMCChemMasterAutoSelectToggleMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemMasterApplyPresetMsg(string presetName, string bottleLabel, RMCPillBottleColors bottleColor, uint pillType, bool usePresetNameAsLabel) : BoundUserInterfaceMessage
{
    public readonly string PresetName = presetName;
    public readonly string BottleLabel = bottleLabel;
    public readonly RMCPillBottleColors BottleColor = bottleColor;
    public readonly uint PillType = pillType;
    public readonly bool UsePresetNameAsLabel = usePresetNameAsLabel;
}
