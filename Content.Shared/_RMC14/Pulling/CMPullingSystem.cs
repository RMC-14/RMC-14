using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Pulling;

public sealed class CMPullingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParalyzeOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);

        SubscribeLocalEvent<SlowOnPullComponent, PullStartedMessage>(OnSlowPullStarted);
        SubscribeLocalEvent<SlowOnPullComponent, PullStoppedMessage>(OnSlowPullStopped);

        SubscribeLocalEvent<PullingSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnPullingSlowedMovementSpeed);

        SubscribeLocalEvent<PullWhitelistComponent, PullAttemptEvent>(OnPullWhitelistPullAttempt);

        SubscribeLocalEvent<BlockPullingDeadComponent, PullAttemptEvent>(OnBlockDeadPullAttempt);
        SubscribeLocalEvent<BlockPullingDeadComponent, PullStartedMessage>(OnBlockDeadPullStarted);
        SubscribeLocalEvent<BlockPullingDeadComponent, PullStoppedMessage>(OnBlockDeadPullStopped);
    }

    private void OnParalyzeOnPullAttempt(Entity<ParalyzeOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<ParalyzeOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsDead(ent))
        {
            return;
        }

        args.Cancelled = true;

        if (ent.Comp.Sound is { } sound)
        {
            var pitch = _random.NextFloat(ent.Comp.MinPitch, ent.Comp.MaxPitch);
            _audio.PlayPredicted(sound, ent, user, sound.Params.WithPitchScale(pitch));
        }

        _stun.TryParalyze(user, ent.Comp.Duration, true);

        foreach (var session in Filter.Pvs(user).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            var puller = Identity.Name(user, EntityManager, recipient);
            var pulled = Identity.Name(ent, EntityManager, recipient);
            var message = $"{puller} tried to pull {pulled} but instead gets a tail swipe to the head!";

            if (args.PullerUid == recipient)
                _popup.PopupClient(message, user, recipient, PopupType.MediumCaution);
            else
                _popup.PopupEntity(message, user, recipient, PopupType.MediumCaution);
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
        if (HasComp<BypassInteractionChecksComponent>(ent) ||
            !TryComp(ent, out PullerComponent? puller) ||
            !TryComp(puller.Pulling, out SlowOnPullComponent? slow))
        {
            return;
        }

        foreach (var slowdown in slow.Slowdowns)
        {
            if (_whitelist.IsWhitelistPass(slowdown.Whitelist, ent))
            {
                args.ModifySpeed(slowdown.Multiplier, slowdown.Multiplier);
                return;
            }
        }

        args.ModifySpeed(slow.Multiplier, slow.Multiplier);
    }

    private void OnPullWhitelistPullAttempt(Entity<PullWhitelistComponent> ent, ref PullAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner == args.PulledUid)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.PulledUid))
        {
            _popup.PopupClient(Loc.GetString("cm-pull-whitelist-denied", ("name", args.PulledUid)), args.PulledUid, args.PullerUid);
            args.Cancelled = true;
        }
    }

    private void OnBlockDeadPullAttempt(Entity<BlockPullingDeadComponent> ent, ref PullAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner == args.PulledUid)
            return;

        if (_mobState.IsDead(args.PulledUid))
        {
            _popup.PopupClient(Loc.GetString("cm-pull-whitelist-denied-dead", ("name", args.PulledUid)), args.PulledUid, args.PullerUid);
            args.Cancelled = true;
        }
    }

    private void OnBlockDeadPullStarted(Entity<BlockPullingDeadComponent> ent, ref PullStartedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            EnsureComp<BlockPullingDeadActiveComponent>(ent);
    }

    private void OnBlockDeadPullStopped(Entity<BlockPullingDeadComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            RemCompDeferred<BlockPullingDeadActiveComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        var blockDeadActive = EntityQueryEnumerator<BlockPullingDeadActiveComponent, PullerComponent>();
        while (blockDeadActive.MoveNext(out var uid, out _, out var puller))
        {
            if (puller.Pulling is not { } pulling ||
                !TryComp(pulling, out PullableComponent? pullable))
            {
                continue;
            }

            if (_mobState.IsDead(pulling))
                _pulling.TryStopPull(pulling, pullable, uid);
        }
    }
}
