using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CryoCell;

[Serializable, NetSerializable]
public sealed class CryoCellBuiState(
    NetEntity? occupant,
    string? occupantName,
    CryoCellOccupantMobState occupantState,
    float health,
    float maxHealth,
    float bruteLoss,
    float burnLoss,
    float toxinLoss,
    float oxyLoss,
    float bodyTemperature,
    float cellTemperature,
    bool isOn,
    bool autoEject,
    bool releaseNotice)
    : BoundUserInterfaceState
{
    public readonly NetEntity? Occupant = occupant;
    public readonly string? OccupantName = occupantName;
    public readonly CryoCellOccupantMobState OccupantState = occupantState;
    public readonly float Health = health;
    public readonly float MaxHealth = maxHealth;
    public readonly float BruteLoss = bruteLoss;
    public readonly float BurnLoss = burnLoss;
    public readonly float ToxinLoss = toxinLoss;
    public readonly float OxyLoss = oxyLoss;
    public readonly float BodyTemperature = bodyTemperature;
    public readonly float CellTemperature = cellTemperature;
    public readonly bool IsOn = isOn;
    public readonly bool AutoEject = autoEject;
    public readonly bool ReleaseNotice = releaseNotice;
}

[Serializable, NetSerializable]
public readonly record struct CryoCellBeakerReagent(string Name, float Volume);

[Serializable, NetSerializable]
public enum CryoCellUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum CryoCellVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum CryoCellVisualState : byte
{
    OffEmpty = 0,
    OffOccupied = 1,
    OnEmpty = 2,
    OnOccupied = 3
}

[Serializable, NetSerializable]
public enum CryoCellVisualLayers
{
    Base
}

[Serializable, NetSerializable]
public enum CryoCellOccupantMobState : byte
{
    None = 0,
    Alive = 1,
    Critical = 2,
    Dead = 3
}

[Serializable, NetSerializable]
public sealed class CryoCellTogglePowerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellToggleAutoEjectBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellEjectBeakerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CryoCellToggleNotifyBuiMsg : BoundUserInterfaceMessage;
