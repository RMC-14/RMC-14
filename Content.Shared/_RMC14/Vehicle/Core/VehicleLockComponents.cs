using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
[Access(typeof(VehicleLockSystem), typeof(VehicleSystem))]
public sealed partial class VehicleLockComponent : Component
{
    [DataField]
    public bool Locked;
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
