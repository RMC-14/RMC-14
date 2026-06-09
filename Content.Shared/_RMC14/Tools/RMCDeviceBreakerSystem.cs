using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Sensor;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Tools;

public sealed class RMCDeviceBreakerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly SharedDestructibleSystem _destroy = default!;
    [Dependency] private readonly SensorTowerSystem _sensor = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDeviceBreakerComponent, RMCDeviceBreakerDoAfterEvent>(OnDeviceBreakerDoafter);
    }

    private void OnDeviceBreakerDoafter(Entity<RMCDeviceBreakerComponent> breaker, ref RMCDeviceBreakerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !CanBreak(args.Target.Value))
            return;

        args.Handled = true;

        Break(args.Target.Value, args.User);

        _audio.PlayPredicted(breaker.Comp.UseSound, breaker, args.User);

        if (breaker.Comp.Repeat && CanBreak(args.Target.Value))
        {
            var doafter = new DoAfterArgs(EntityManager, args.User, breaker.Comp.DoAfterTime, new RMCDeviceBreakerDoAfterEvent(), args.Used, args.Target, args.Used)
            {
                BreakOnMove = true,
                RequireCanInteract = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            _doafter.TryStartDoAfter(doafter);
        }
    }

    private bool CanBreak(EntityUid target)
    {
        if (TryComp<RMCFusionReactorComponent>(target, out var reactor) && reactor.State != RMCFusionReactorState.Weld)
            return true;

        if (TryComp<CommunicationsTowerComponent>(target, out var comms) && comms.State != CommunicationsTowerState.Broken)
            return true;

        if (TryComp<SensorTowerComponent>(target, out var sensors) && sensors.State != SensorTowerState.Weld)
            return true;

        return false;
    }

    private void Break(EntityUid target, EntityUid user)
    {
        if (TryComp<RMCFusionReactorComponent>(target, out var reactor) && reactor.State != RMCFusionReactorState.Weld)
        {
            _power.DestroyReactor((target, reactor), user);
        }

        if (TryComp<CommunicationsTowerComponent>(target, out var comms) && comms.State != CommunicationsTowerState.Broken)
        {
            _destroy.BreakEntity(target);
        }

        if (TryComp<SensorTowerComponent>(target, out var sensors) && sensors.State != SensorTowerState.Weld)
        {
            _sensor.SensorTowerIncrementalDestroy((target, sensors));
        }
    }
}
