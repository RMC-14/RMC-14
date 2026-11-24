using System;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleOperatorInputCmdHandler(VehicleSystem vehicles, Action<EntityUid> onPressed) : InputCmdHandler
{
    public override bool FireOutsidePrediction => true;

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (message.State != BoundKeyState.Down)
            return false;

        if (session?.AttachedEntity is not { } user ||
            !vehicles.TryResolveControlledVehicle(user, out var vehicle) ||
            !entManager.TryGetComponent(vehicle, out VehicleComponent? vehicleComp) ||
            vehicleComp.Operator != user)
        {
            return false;
        }

        onPressed(vehicle);
        return true;
    }
}
