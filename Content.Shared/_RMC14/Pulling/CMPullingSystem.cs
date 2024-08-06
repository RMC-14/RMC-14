using Content.Shared._RMC14.Xenonids.Parasite;
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
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParalyzeOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);
        SubscribeLocalEvent<InfectOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);

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

        var puller = user;
        var pulled = target;
        var othersMessage = Loc.GetString("rmc-pull-paralyze-others", ("puller", puller), ("pulled", pulled));
        var selfMessage = Loc.GetString("rmc-pull-paralyze-self", ("puller", puller), ("pulled", pulled));

        _popup.PopupPredicted(selfMessage, othersMessage, puller, puller, PopupType.MediumCaution);
    }

    private void OnParalyzeOnPullAttempt(Entity<InfectOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<InfectOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsDead(ent))
        {
            return;
        }

        if (!TryComp<XenoParasiteComponent>(target, out var paraComp))
            return;

        Entity<XenoParasiteComponent> comp = (target, paraComp);

        if (!_parasite.Infect(comp, user, false, true))
            return;

        args.Cancelled = true;

        var puller = user;
        var pulled = target;
        var othersMessage = Loc.GetString("rmc-pull-infect-others", ("puller", puller), ("pulled", pulled));
        var selfMessage = Loc.GetString("rmc-pull-infect-self", ("puller", puller), ("pulled", pulled));

        _popup.PopupPredicted(selfMessage, othersMessage, puller, puller, PopupType.MediumCaution);
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
