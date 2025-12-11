using System;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Handles activating vehicle overcharge via input.
/// </summary>
public sealed class RMCVehicleOverchargeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleOperatorComponent, MoveInputEvent>(OnOperatorMoveInput);
    }

    private void OnOperatorMoveInput(Entity<VehicleOperatorComponent> ent, ref MoveInputEvent args)
    {
        if (_net.IsClient)
            return;

        var held = args.Entity.Comp.HeldMoveButtons;
        var walkHeld = (held & MoveButtons.Walk) != 0;
        if (!walkHeld)
            return;

        var vehicle = ent.Comp.Vehicle;
        if (vehicle == null || !TryComp(vehicle, out RMCVehicleOverchargeComponent? overcharge))
            return;

        var now = _timing.CurTime;

        if (overcharge.CooldownUntil > now || overcharge.ActiveUntil > now)
            return;

        overcharge.ActiveUntil = now + TimeSpan.FromSeconds(overcharge.Duration);
        overcharge.CooldownUntil = now + TimeSpan.FromSeconds(overcharge.Cooldown);

        if (overcharge.OverchargeSound != null)
            _audio.PlayPvs(overcharge.OverchargeSound, vehicle.Value);

        Dirty(vehicle.Value, overcharge);
    }
}
