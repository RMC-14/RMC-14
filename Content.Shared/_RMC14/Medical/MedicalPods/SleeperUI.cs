using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.MedicalPods;

[Serializable, NetSerializable]
public sealed class SleeperBuiState : BoundUserInterfaceState
{
    public readonly NetEntity? Occupant;
    public readonly string? OccupantName;
    public readonly int OccupantStat;
    public readonly float OccupantHealth;
    public readonly float OccupantMaxHealth;
    public readonly float OccupantMinHealth;
    public readonly float BruteLoss;
    public readonly float BurnLoss;
    public readonly float ToxinLoss;
    public readonly float OxyLoss;
    public readonly bool HasBlood;
    public readonly FixedPoint2 BloodLevel;
    public readonly FixedPoint2 BloodMax;
    public readonly float BloodPercent;
    public readonly float BodyTemperature;
    public readonly bool Filtering;
    public readonly FixedPoint2 TotalReagents;
    public readonly FixedPoint2 ReagentsWhenStarted;
    public readonly bool AutoEjectDead;
    public readonly FixedPoint2 MaxChem;
    public readonly float MinHealth;
    public readonly IReadOnlyList<SleeperChemicalData> Chemicals;
    public readonly IReadOnlyList<int> InjectionAmounts;

    public SleeperBuiState(
        NetEntity? occupant,
        string? occupantName,
        int occupantStat,
        float occupantHealth,
        float occupantMaxHealth,
        float occupantMinHealth,
        float bruteLoss,
        float burnLoss,
        float toxinLoss,
        float oxyLoss,
        bool hasBlood,
        FixedPoint2 bloodLevel,
        FixedPoint2 bloodMax,
        float bloodPercent,
        float bodyTemperature,
        bool filtering,
        FixedPoint2 totalReagents,
        FixedPoint2 reagentsWhenStarted,
        bool autoEjectDead,
        FixedPoint2 maxChem,
        float minHealth,
        IReadOnlyList<SleeperChemicalData> chemicals,
        IReadOnlyList<int> injectionAmounts)
    {
        Occupant = occupant;
        OccupantName = occupantName;
        OccupantStat = occupantStat;
        OccupantHealth = occupantHealth;
        OccupantMaxHealth = occupantMaxHealth;
        OccupantMinHealth = occupantMinHealth;
        BruteLoss = bruteLoss;
        BurnLoss = burnLoss;
        ToxinLoss = toxinLoss;
        OxyLoss = oxyLoss;
        HasBlood = hasBlood;
        BloodLevel = bloodLevel;
        BloodMax = bloodMax;
        BloodPercent = bloodPercent;
        BodyTemperature = bodyTemperature;
        Filtering = filtering;
        TotalReagents = totalReagents;
        ReagentsWhenStarted = reagentsWhenStarted;
        AutoEjectDead = autoEjectDead;
        MaxChem = maxChem;
        MinHealth = minHealth;
        Chemicals = chemicals;
        InjectionAmounts = injectionAmounts;
    }
}

[Serializable, NetSerializable]
public readonly record struct SleeperChemicalData(
    string Name,
    ProtoId<ReagentPrototype> Id,
    FixedPoint2 OccupantAmount,
    bool Injectable,
    bool Overdosing,
    bool OdWarning);

[Serializable, NetSerializable]
public enum SleeperUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SleeperInjectChemicalBuiMsg(ProtoId<ReagentPrototype> chemical, int amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype> Chemical = chemical;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class SleeperToggleFilterBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SleeperEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SleeperAutoEjectDeadBuiMsg(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public enum SleeperVisuals : byte
{
    Occupied
}

[Serializable, NetSerializable]
public enum SleeperVisualLayers
{
    Base
}
