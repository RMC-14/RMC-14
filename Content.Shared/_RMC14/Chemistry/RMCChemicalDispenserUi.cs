using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry;

[Serializable, NetSerializable]
public enum RMCChemicalDispenserUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCChemicalDispenserDispenseSettingBuiMsg(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCChemicalDispenserBeakerBuiMsg(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCChemicalDispenserEjectBeakerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCChemicalDispenserDispenseBuiMsg(ProtoId<ReagentPrototype> reagent) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype> Reagent = reagent;
}
