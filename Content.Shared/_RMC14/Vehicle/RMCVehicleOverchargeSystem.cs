using System;
using Content.Shared._RMC14.Input;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleOverchargeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCVehicleSystem _rmcVehicles = default!;

    public override void Initialize()
    {
        if (_net.IsClient)
        {
            CommandBinds.Builder
                .Bind(CMKeyFunctions.CMUniqueAction, InputCmdHandler.FromDelegate(session =>

                {
                    if (session?.AttachedEntity is not { } user)
                        return;

                    EntityUid? vehicleUid = null;
                    if (TryComp<VehicleOperatorComponent>(user, out var op) && op.Vehicle != null)
                    {
                        vehicleUid = op.Vehicle.Value;
                    }
                    else if (_rmcVehicles.TryGetVehicleFromInterior(user, out var interiorVehicle) && interiorVehicle != null)
                    {
                        vehicleUid = interiorVehicle.Value;
                    }

                    if (vehicleUid == null)
                        return;

                    RaiseNetworkEvent(new RMCVehicleOverchargeRequestEvent(GetNetEntity(vehicleUid.Value)));
                }, handle: false))
                .Register<RMCVehicleOverchargeSystem>();
        }

        SubscribeNetworkEvent<RMCVehicleOverchargeRequestEvent>(OnOverchargeRequest);
    }

    private void OnOverchargeRequest(RMCVehicleOverchargeRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var vehicle = GetEntity(ev.Vehicle);

        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator != user)
            return;

        if (!TryComp(vehicle, out RMCVehicleOverchargeComponent? overcharge))
            return;

        var now = _timing.CurTime;

        if (overcharge.CooldownUntil > now || overcharge.ActiveUntil > now)
            return;

        overcharge.ActiveUntil = now + TimeSpan.FromSeconds(overcharge.Duration);
        overcharge.CooldownUntil = now + TimeSpan.FromSeconds(overcharge.Cooldown);

        if (overcharge.OverchargeSound != null)
            _audio.PlayPvs(overcharge.OverchargeSound, vehicle);

        Dirty(vehicle, overcharge);
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCVehicleOverchargeRequestEvent : EntityEventArgs
{
    public NetEntity Vehicle;

    public RMCVehicleOverchargeRequestEvent()
    {
    }

    public RMCVehicleOverchargeRequestEvent(NetEntity vehicle)
    {
        Vehicle = vehicle;
    }
}
