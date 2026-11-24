using System;
using Content.Shared._RMC14.Input;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleHornSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly VehicleSystem _rmcVehicles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        if (_net.IsClient)
        {
            CommandBinds.Builder
                .Bind(CMKeyFunctions.RMCVehicleHorn, new VehicleOperatorInputCmdHandler(_rmcVehicles,
                    vehicle => RaiseNetworkEvent(new VehicleHornRequestEvent(GetNetEntity(vehicle)))))
                .Register<VehicleHornSystem>();
        }

        SubscribeNetworkEvent<VehicleHornRequestEvent>(OnHornRequest);
    }

    public override void Shutdown()
    {
        if (_net.IsClient)
            CommandBinds.Unregister<VehicleHornSystem>();
    }

    private void OnHornRequest(VehicleHornRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var vehicle = GetEntity(ev.Vehicle);

        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator != user)
            return;

        if (!TryComp(vehicle, out VehicleSoundComponent? sound) || sound.HornSound == null)
            return;

        var now = _timing.CurTime;
        if (sound.NextHornSound > now)
            return;

        sound.NextHornSound = now + TimeSpan.FromSeconds(sound.HornCooldown);
        _audio.PlayPvs(sound.HornSound, vehicle);
        Dirty(vehicle, sound);
    }
}

[Serializable, NetSerializable]
public sealed partial class VehicleHornRequestEvent : EntityEventArgs
{
    public NetEntity Vehicle;

    public VehicleHornRequestEvent()
    {
    }

    public VehicleHornRequestEvent(NetEntity vehicle)
    {
        Vehicle = vehicle;
    }
}
