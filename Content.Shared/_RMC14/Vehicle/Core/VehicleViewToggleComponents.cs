using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleViewToggleSystem))]
public sealed partial class VehicleViewToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionVehicleToggleView";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? OutsideTarget;

    [DataField, AutoNetworkedField]
    public EntityUid? InsideTarget;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    [DataField, AutoNetworkedField]
    public bool IsOutside;

    [DataField]
    public HashSet<EntityUid> Sources = new();
}

public sealed partial class VehicleToggleViewActionEvent : InstantActionEvent;

public sealed class VehicleViewToggledEvent : EntityEventArgs
{
    public readonly bool IsOutside;

    public VehicleViewToggledEvent(bool isOutside)
    {
        IsOutside = isOutside;
    }
}
