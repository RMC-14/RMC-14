using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Stores state for the synthetic maintenance station repair and charging cycle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSyntheticMaintenanceStationComponent : Component
{
    /// <summary>
    /// Container slot holding the synthetic currently inside the station.
    /// </summary>
    public const string BodyContainerId = "rmc_synthetic_maintenance_station_body";

    /// <summary>
    /// Runtime container for the occupant.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Networked occupancy state used by shared drag-drop validation and visuals.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Occupied;

    /// <summary>
    /// Maximum internal charge stored by the station.
    /// </summary>
    [DataField]
    public float MaxInternalCharge = 15000;

    /// <summary>
    /// Current internal charge available for maintenance work.
    /// </summary>
    [DataField]
    public float CurrentInternalCharge = 15000;

    /// <summary>
    /// Charge restored per update while powered and empty.
    /// </summary>
    [DataField]
    public float PassiveRechargeRate = 2500;

    /// <summary>
    /// Charge restored per update while powered and occupied.
    /// </summary>
    [DataField]
    public float ActiveRechargeRate = 25000;

    /// <summary>
    /// Charge lost per update while unpowered.
    /// </summary>
    [DataField]
    public float UnpoweredDrainRate = 50;

    /// <summary>
    /// Charge consumed by each successful repair or blood restore tick.
    /// </summary>
    [DataField]
    public float RepairChargeCost = 500;

    /// <summary>
    /// Delay between station processing ticks.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Do-after length for inserting another synth into the station.
    /// </summary>
    [DataField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Stun applied when a synth exits the station.
    /// </summary>
    [DataField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Negative damage applied each repair tick.
    /// </summary>
    [DataField]
    public DamageSpecifier RepairDamage = new()
    {
        DamageDict =
        {
            ["Blunt"] = -10,
            ["Piercing"] = -10,
            ["Slash"] = -10,
            ["Heat"] = -10,
            ["Shock"] = -10,
            ["Cold"] = -10,
            ["Caustic"] = -10,
        },
    };

    /// <summary>
    /// Amount of synthetic blood/fluid restored per maintenance tick.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodRestoreAmount = 10;

    /// <summary>
    /// Next game time when the station should process.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdate;
}

/// <summary>
/// Visual data keys for the maintenance station.
/// </summary>
[Serializable, NetSerializable]
public enum RMCSyntheticMaintenanceStationVisuals
{
    Status,
    Charge,
}

/// <summary>
/// Sprite layers controlled by the maintenance station visualizer.
/// </summary>
[Serializable, NetSerializable]
public enum RMCSyntheticMaintenanceStationLayers
{
    Base,
    Charge,
}

/// <summary>
/// Powered and occupancy state shown by the station.
/// </summary>
[Serializable, NetSerializable]
public enum RMCSyntheticMaintenanceStationStatus
{
    Off,
    Empty,
    Occupied,
}

/// <summary>
/// Coarse charge levels used for overlay states.
/// </summary>
[Serializable, NetSerializable]
public enum RMCSyntheticMaintenanceStationCharge
{
    Empty,
    Low,
    Medium,
    High,
    Full,
}

/// <summary>
/// Do-after event for placing another synthetic into a maintenance station.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RMCSyntheticMaintenanceStationInsertDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Entity being inserted when the do-after completes.
    /// </summary>
    [DataField]
    public NetEntity Inserted;

    public RMCSyntheticMaintenanceStationInsertDoAfterEvent()
    {
    }

    public RMCSyntheticMaintenanceStationInsertDoAfterEvent(NetEntity inserted)
    {
        Inserted = inserted;
    }
}
