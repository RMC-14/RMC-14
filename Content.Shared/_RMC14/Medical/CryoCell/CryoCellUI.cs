using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CryoCell;

[Serializable, NetSerializable]
public enum CryoCellUIKey
{
    Key,
}

[Serializable, NetSerializable]
public enum CryoCellVisuals : byte
{
    Occupied,
}

[Serializable, NetSerializable]
public enum CryoCellVisualLayers
{
    Base,
}

[Serializable, NetSerializable]
public sealed class CryoCellBuiState(
    NetEntity? occupant,
    string? occupantName,
    float health,
    float maxHealth,
    float bodyTemperature,
    bool isOperating,
    bool hasBeaker,
    bool autoEject,
    bool notify)
    : BoundUserInterfaceState
{
    public readonly NetEntity? Occupant = occupant;
    public readonly string? OccupantName = occupantName;
    public readonly float Health = health;
    public readonly float MaxHealth = maxHealth;
    public readonly float BodyTemperature = bodyTemperature;
    public readonly bool IsOperating = isOperating;
    public readonly bool HasBeaker = hasBeaker;
    public readonly bool AutoEject = autoEject;
    public readonly bool Notify = notify;
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
