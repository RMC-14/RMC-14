using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleViewToggleSystem))]
public sealed partial class RMCVehicleViewToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionRMCVehicleToggleView";

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
}

public sealed partial class RMCVehicleToggleViewActionEvent : InstantActionEvent;
