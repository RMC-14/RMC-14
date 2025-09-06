using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Wheelchair;

public sealed class WheelchairSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WheelchairComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<WheelchairComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<WheelchairComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<ActiveWheelchairPilotComponent, RingBellActionEvent>(OnRingBell);

        SubscribeLocalEvent<ActiveWheelchairPilotComponent, PreventCollideEvent>(OnActivePilotPreventCollide);
        SubscribeLocalEvent<ActiveWheelchairPilotComponent, KnockedDownEvent>(OnActivePilotStunned);
        SubscribeLocalEvent<ActiveWheelchairPilotComponent, StunnedEvent>(OnActivePilotStunned);
        SubscribeLocalEvent<ActiveWheelchairPilotComponent, MobStateChangedEvent>(OnActivePilotMobStateChanged);
    }

    private void OnRefreshSpeed(Entity<WheelchairComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp(ent, out StrapComponent? strap) || strap.BuckledEntities.Count == 0)
            return;

        var speed = ent.Comp.SpeedMultiplier;
        args.ModifySpeed(speed, speed);
    }

    private void OnStrapped(Entity<WheelchairComponent> ent, ref StrappedEvent args)
    {
        var buckle = args.Buckle;
        var pilot = EnsureComp<ActiveWheelchairPilotComponent>(buckle);

        _mover.SetRelay(buckle, ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (ent.Comp.BellAction != null)
        {
            pilot.BellActionEntity = _actions.AddAction(buckle, ent.Comp.BellAction);
        }
    }

    private void OnUnstrapped(Entity<WheelchairComponent> ent, ref UnstrappedEvent args)
    {
        var buckle = args.Buckle;
        
        if (TryComp<ActiveWheelchairPilotComponent>(buckle, out var pilot) && pilot.BellActionEntity != null)
        {
            _actions.RemoveAction(buckle.Owner, pilot.BellActionEntity.Value);
        }
        
        RemCompDeferred<ActiveWheelchairPilotComponent>(buckle);
        RemCompDeferred<RelayInputMoverComponent>(buckle);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnActivePilotPreventCollide(Entity<ActiveWheelchairPilotComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnActivePilotStunned<T>(Entity<ActiveWheelchairPilotComponent> ent, ref T args)
    {
        RemovePilot(ent);
    }

    private void OnActivePilotMobStateChanged(Entity<ActiveWheelchairPilotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            OnActivePilotStunned(ent, ref args);
    }

    private void RemovePilot(Entity<ActiveWheelchairPilotComponent> active)
    {
        _buckle.Unbuckle(active.Owner, null);
        RemCompDeferred<ActiveWheelchairPilotComponent>(active);
    }

    public override void Update(float frameTime)
    {
        var pilots = EntityQueryEnumerator<ActiveWheelchairPilotComponent>();
        while (pilots.MoveNext(out var uid, out var active))
        {
            if (!TryComp(uid, out BuckleComponent? buckle) ||
                !HasComp<WheelchairComponent>(buckle.BuckledTo))
            {
                RemovePilot((uid, active));
            }
        }
    }

    private void OnRingBell(Entity<ActiveWheelchairPilotComponent> ent, ref RingBellActionEvent args)
    {
        if (args.Handled || !TryComp(ent, out BuckleComponent? buckle) || !TryComp(buckle.BuckledTo, out WheelchairComponent? wheelchair))
            return;

        args.Handled = true;
        _audio.PlayPredicted(wheelchair.BellSound, args.Performer, args.Performer);
    }
}
