using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Autodoc;

[Serializable, NetSerializable]
public sealed class AutodocBuiState(
    NetEntity? occupant,
    string? occupantName,
    AutodocOccupantMobState occupantState,
    float health,
    float maxHealth,
    float bruteLoss,
    float burnLoss,
    float toxinLoss,
    float oxyLoss,
    FixedPoint2 bloodLevel,
    float bloodPercent,
    string pulse,
    bool surgeryInProgress,
    bool healingBrute,
    bool healingBurn,
    bool healingToxin,
    bool bloodTransfusion,
    bool filtering,
    FixedPoint2 totalReagents,
    bool removeLarva,
    bool closeIncisions,
    bool removeShrapnel,
    bool internalBleeding,
    bool brokenBone,
    bool organDamage,
    HashSet<AutodocUpgradeTier> installedUpgrades)
    : BoundUserInterfaceState
{
    public readonly NetEntity? Occupant = occupant;
    public readonly string? OccupantName = occupantName;
    public readonly AutodocOccupantMobState OccupantState = occupantState;
    public readonly float Health = health;
    public readonly float MaxHealth = maxHealth;
    public readonly float BruteLoss = bruteLoss;
    public readonly float BurnLoss = burnLoss;
    public readonly float ToxinLoss = toxinLoss;
    public readonly float OxyLoss = oxyLoss;
    public readonly FixedPoint2 BloodLevel = bloodLevel;
    public readonly float BloodPercent = bloodPercent;
    public readonly string Pulse = pulse;
    public readonly bool SurgeryInProgress = surgeryInProgress;
    public readonly bool HealingBrute = healingBrute;
    public readonly bool HealingBurn = healingBurn;
    public readonly bool HealingToxin = healingToxin;
    public readonly bool BloodTransfusion = bloodTransfusion;
    public readonly bool Filtering = filtering;
    public readonly FixedPoint2 TotalReagents = totalReagents;
    public readonly bool RemoveLarva = removeLarva;
    public readonly bool CloseIncisions = closeIncisions;
    public readonly bool RemoveShrapnel = removeShrapnel;
    public readonly bool InternalBleeding = internalBleeding;
    public readonly bool BrokenBone = brokenBone;
    public readonly bool OrganDamage = organDamage;
    public readonly HashSet<AutodocUpgradeTier> InstalledUpgrades = installedUpgrades;
}

[Serializable, NetSerializable]
public enum AutodocUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum AutodocOccupantMobState : byte
{
    None = 0,
    Alive = 1,
    Critical = 2,
    Dead = 3
}

[Serializable, NetSerializable]
public enum AutodocVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum AutodocVisualState : byte
{
    Empty = 0,
    Occupied = 1,
    Operating = 2
}

[Serializable, NetSerializable]
public enum AutodocVisualLayers
{
    Base
}

[Serializable, NetSerializable]
public sealed class AutodocToggleBruteBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleBurnBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleBloodBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleDialysisBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleToxinBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleCloseIncisionsBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleRemoveShrapnelBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleInternalBleedingBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleBrokenBoneBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleOrganDamageBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocToggleLarvaBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocStartSurgeryBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocClearBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AutodocImportScanBuiMsg : BoundUserInterfaceMessage;
