using System;
using System.Collections.Generic;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleLockSystem), typeof(VehicleSystem))]
public sealed partial class VehicleLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField, AutoNetworkedField]
    public string? KeyId;

    [DataField]
    public ProtoId<ToolQualityPrototype> BreakToolQuality = "Cutting";

    [DataField]
    public TimeSpan BreakDelay = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan BreakAlarmInterval = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan BreakAlarmFlashDuration = TimeSpan.FromSeconds(0.6);

    [DataField]
    public ProtoId<ToolQualityPrototype> RepairToolQuality = "Screwing";

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(10);

    public int AlarmToken;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleLockSystem))]
public sealed partial class VehicleLockActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionVehicleLock";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;

    [DataField]
    public HashSet<EntityUid> Sources = new();
}

public sealed partial class VehicleLockActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class VehicleLockBreakDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VehicleLockRepairDoAfterEvent : SimpleDoAfterEvent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleLockSystem))]
public sealed partial class VehicleKeyComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? KeyId;

    [DataField, AutoNetworkedField]
    public VehicleKeyMode Mode = VehicleKeyMode.Normal;
}

[Serializable, NetSerializable]
public enum VehicleKeyMode : byte
{
    Normal,
    Blank,
    Duplicator,
}
