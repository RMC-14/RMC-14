using System;
using Content.Shared._RMC14.Input;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleSafetySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly VehicleSystem _rmcVehicles = default!;

    public override void Initialize()
    {
        if (_net.IsClient)
        {
            CommandBinds.Builder
                .Bind(CMKeyFunctions.RMCVehicleSafety, new VehicleOperatorInputCmdHandler(_rmcVehicles, RequestToggle))
                .Register<VehicleSafetySystem>();
        }

        SubscribeAllEvent<VehicleSafetyToggleRequestEvent>(OnSafetyToggleRequest);
    }

    public override void Shutdown()
    {
        if (_net.IsClient)
            CommandBinds.Unregister<VehicleSafetySystem>();
    }

    private void OnSafetyToggleRequest(VehicleSafetyToggleRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryGetEntity(ev.Vehicle, out var vehicle) ||
            !TryComp(vehicle, out VehicleComponent? vehicleComp) ||
            vehicleComp.Operator != user ||
            !TryComp(vehicle, out VehicleSafetyComponent? safety))
        {
            return;
        }

        safety.Enabled = !safety.Enabled;
        Dirty(vehicle.Value, safety);
    }

    public void RequestToggle(EntityUid vehicle)
    {
        RaisePredictiveEvent(new VehicleSafetyToggleRequestEvent(GetNetEntity(vehicle)));
    }

    public bool IsEnabled(Entity<VehicleSafetyComponent?> vehicle)
    {
        return Resolve(vehicle, ref vehicle.Comp, false) && vehicle.Comp.Enabled;
    }
}

[Serializable, NetSerializable]
public sealed partial class VehicleSafetyToggleRequestEvent : EntityEventArgs
{
    public NetEntity Vehicle;

    public VehicleSafetyToggleRequestEvent()
    {
    }

    public VehicleSafetyToggleRequestEvent(NetEntity vehicle)
    {
        Vehicle = vehicle;
    }
}
