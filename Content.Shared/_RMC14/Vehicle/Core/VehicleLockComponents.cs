using System;
using System.Collections.Generic;
using Content.Shared.Access;
using Content.Shared.Actions;
using Content.Shared.NPC.Prototypes;
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
    public string? KeyId;

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> Access = new() { "CMAccessCrewman", "RMCAccessCommand", "CMAccessBrig" };

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
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
