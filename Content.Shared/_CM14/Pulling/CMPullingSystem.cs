using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Pulling;

public sealed class CMPullingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParalyzeOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);

        SubscribeLocalEvent<SlowOnPullComponent, PullStartedMessage>(OnSlowPullStarted);
        SubscribeLocalEvent<SlowOnPullComponent, PullStoppedMessage>(OnSlowPullStopped);

        SubscribeLocalEvent<PullingSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnPullingSlowedMovementSpeed);
    }

    private void OnParalyzeOnPullAttempt(Entity<ParalyzeOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<ParalyzeOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsIncapacitated(ent))
        {
            return;
        }

        _stun.TryParalyze(user, ent.Comp.Duration, true);
        args.Cancelled = true;

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var session in Filter.Pvs(user).Recipients)
        {
            if (session == IoCManager.Resolve<ISharedPlayerManager>().LocalSession)
                continue;

            var puller = Identity.Name(user, EntityManager, session.AttachedEntity);
            var pulled = Identity.Name(ent, EntityManager, session.AttachedEntity);
            var message = $"{puller} tried to pull {pulled} but instead gets a tail swipe to the head!";
            _popup.PopupEntity(message, user, session, PopupType.MediumCaution);
        }
    }

    private void OnSlowPullStarted(Entity<SlowOnPullComponent> ent, ref PullStartedMessage args)
    {
        if (ent.Owner == args.PulledUid)
        {
            EnsureComp<PullingSlowedComponent>(args.PullerUid);
            _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
        }
    }

    private void OnSlowPullStopped(Entity<SlowOnPullComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner == args.PulledUid)
        {
            RemCompDeferred<PullingSlowedComponent>(args.PullerUid);
            _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
        }
    }

    private void OnPullingSlowedMovementSpeed(Entity<PullingSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp(ent, out PullerComponent? puller) &&
            TryComp(puller.Pulling, out SlowOnPullComponent? slow))
        {
            args.ModifySpeed(slow.Multiplier, slow.Multiplier);
        }
    }
}
