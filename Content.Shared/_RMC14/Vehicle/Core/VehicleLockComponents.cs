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

    [DataField]
    public ProtoId<ToolQualityPrototype> BreakToolQuality = "Cutting";

    [DataField]
    public TimeSpan BreakDelay = TimeSpan.FromSeconds(30);

    [DataField]
    public ProtoId<ToolQualityPrototype> RepairToolQuality = "Screwing";

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(10);
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
