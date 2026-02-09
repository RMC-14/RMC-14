using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.MedicalPods;

[Serializable, NetSerializable]
public sealed class SleeperBuiState(
    NetEntity? occupant,
    string? occupantName,
    SleeperOccupantMobState occupantState,
    float health,
    float maxHealth,
    float minHealth,
    float bruteLoss,
    float burnLoss,
    float toxinLoss,
    float oxyLoss,
    float geneticLoss,
    bool hasBlood,
    FixedPoint2 bloodLevel,
    float bloodPercent,
    int pulse,
    float bodyTemperature,
    bool filtering,
    FixedPoint2 totalReagents,
    FixedPoint2 reagentsWhenStarted,
    bool autoEjectDead,
    FixedPoint2 maxChem,
    float crisisMinDamage,
    SleeperChemicalData[] chemicals,
    int[] injectionAmounts)
    : BoundUserInterfaceMessage
{
    public readonly NetEntity? Occupant = occupant;
    public readonly string? OccupantName = occupantName;
    public readonly SleeperOccupantMobState OccupantState = occupantState;
    public readonly float Health = health;
    public readonly float MaxHealth = maxHealth;
    public readonly float MinHealth = minHealth;
    public readonly float BruteLoss = bruteLoss;
    public readonly float BurnLoss = burnLoss;
    public readonly float ToxinLoss = toxinLoss;
    public readonly float OxyLoss = oxyLoss;
    public readonly float GeneticLoss = geneticLoss;
    public readonly bool HasBlood = hasBlood;
    public readonly FixedPoint2 BloodLevel = bloodLevel;
    public readonly float BloodPercent = bloodPercent;
    public readonly int Pulse = pulse;
    public readonly float BodyTemperature = bodyTemperature;
    public readonly bool Filtering = filtering;
    public readonly FixedPoint2 TotalReagents = totalReagents;
    public readonly FixedPoint2 ReagentsWhenStarted = reagentsWhenStarted;
    public readonly bool AutoEjectDead = autoEjectDead;
    public readonly FixedPoint2 MaxChem = maxChem;
    public readonly float CrisisMinDamage = crisisMinDamage;
    public readonly SleeperChemicalData[] Chemicals = chemicals;
    public readonly int[] InjectionAmounts = injectionAmounts;
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
    Occupied,
    OccupantHealthState
}

[Serializable, NetSerializable]
public enum SleeperOccupantMobState : byte
{
    None = 0,
    Alive = 1,
    Critical = 2,
    Dead = 3
}

[Serializable, NetSerializable]
public enum SleeperVisualLayers
{
    Base
}
