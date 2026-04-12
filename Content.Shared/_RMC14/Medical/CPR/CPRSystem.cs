using System.Linq;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.ShakeStun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CPR;

public sealed class CPRSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;

    // TODO RMC14 move these to a component
    [ValidatePrototypeId<DamageTypePrototype>]
    private const string HealType = "Asphyxiation";

    private static readonly TimeSpan CPRCooldownSeconds = TimeSpan.FromSeconds(7);
    private static readonly FixedPoint2 HealAmount = FixedPoint2.New(10);
    private static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();

        // TODO RMC14 something more generic than "marine"
        SubscribeLocalEvent<MarineComponent, InteractHandEvent>(OnMarineInteractHand,
            before: [typeof(InteractionPopupSystem), typeof(StunShakeableSystem)]);
        SubscribeLocalEvent<MarineComponent, CPRDoAfterEvent>(OnMarineDoAfter);

        SubscribeLocalEvent<ReceivingCPRComponent, ReceiveCPRAttemptEvent>(OnReceivingCPRAttempt);
        SubscribeLocalEvent<CPRReceivedComponent, ReceiveCPRAttemptEvent>(OnReceivedCPRAttempt);
        SubscribeLocalEvent<MobStateComponent, ReceiveCPRAttemptEvent>(OnMobStateCPRAttempt);

        SubscribeLocalEvent<CPRDummyComponent, UseInHandEvent>(OnDummyUseInHand);
        SubscribeLocalEvent<CPRDummyComponent, InteractHandEvent>(OnDummyInteractHand,
            before: [typeof(InteractionPopupSystem), typeof(StunShakeableSystem)]);
        SubscribeLocalEvent<CPRDummyComponent, ExaminedEvent>(OnDummyExamined);
        SubscribeLocalEvent<CPRDummyComponent, GetVerbsEvent<AlternativeVerb>>(OnDummyGetAlternativeVerbs);
    }

    private void OnMarineInteractHand(Entity<MarineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartCPR(args.User, args.Target);
    }

    private void OnMarineDoAfter(Entity<MarineComponent> ent, ref CPRDoAfterEvent args)
    {
        var performer = args.User;

        if (args.Target != null)
            RemComp<ReceivingCPRComponent>(args.Target.Value);

        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !CanCPRPopup(performer, target, false, out var damage))
        {
            return;
        }

        args.Handled = true;

        if (TryComp(target, out CPRDummyComponent? dummy))
        {
            HandleDummyCPR((target, dummy), performer);
            return;
        }

        _unrevivable.AddRevivableTime(target, CPRCooldownSeconds);

        if (!TryComp(target, out DamageableComponent? damageable) ||
            !damageable.Damage.DamageDict.TryGetValue(HealType, out damage))
        {
            return;
        }

        var heal = -FixedPoint2.Min(damage, HealAmount);
        var healSpecifier = new DamageSpecifier();
        healSpecifier.DamageDict.Add(HealType, heal);
        _damageable.TryChangeDamage(target, healSpecifier, true);

        var received = EnsureComp<CPRReceivedComponent>(target);
        received.Last = _timing.CurTime;

        if (_net.IsClient)
            return;

        var selfPopup = Loc.GetString("cm-cpr-self-perform", ("target", target), ("seconds", (int)CPRCooldownSeconds.TotalSeconds));
        _popups.PopupEntity(selfPopup, target, performer, PopupType.Medium);

        var othersPopup = Loc.GetString("cm-cpr-other-perform", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.Medium);
    }

    private void OnReceivingCPRAttempt(Entity<ReceivingCPRComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        // If stale, remove the component and allow the new CPR attempt
        if (_timing.CurTime - ent.Comp.StartTime > CPRCooldownSeconds)
        {
            RemCompDeferred<ReceivingCPRComponent>(ent);
            return;
        }

        args.Cancelled = true;

        if (_net.IsClient)
            return;

        var popup = Loc.GetString("cm-cpr-already-being-performed", ("target", ent.Owner));
        _popups.PopupEntity(popup, ent, args.Performer, PopupType.Medium);
    }

    private void OnReceivedCPRAttempt(Entity<CPRReceivedComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        if (args.Start)
            return;

        var target = ent.Owner;
        var performer = args.Performer;

        if (!_mobState.IsDead(ent) ||
            ent.Comp.Last <= _timing.CurTime - CPRCooldownSeconds)
        {
            return;
        }

        args.Cancelled = true;

        if (_net.IsClient)
            return;

        var selfPopup = Loc.GetString("cm-cpr-self-perform-fail-received-too-recently", ("target", target));
        _popups.PopupEntity(selfPopup, target, performer, PopupType.MediumCaution);

        var othersPopup = Loc.GetString("cm-cpr-other-perform-fail", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.MediumCaution);
    }

    private void OnMobStateCPRAttempt(Entity<MobStateComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_mobState.IsAlive(ent) ||
            (_mobState.IsDead(ent) && _unrevivable.IsUnrevivable(ent)))
        {
            args.Cancelled = true;
        }
    }

    private bool CanCPRPopup(EntityUid performer, EntityUid target, bool start, out FixedPoint2 damage)
    {
        damage = default;

        if (!HasComp<MarineComponent>(performer))
            return false;

        if (!HasComp<MarineComponent>(target) && !HasComp<CPRDummyComponent>(target))
            return false;

        var performAttempt = new PerformCPRAttemptEvent(target);
        RaiseLocalEvent(performer, ref performAttempt);

        if (performAttempt.Cancelled)
            return false;

        var receiveAttempt = new ReceiveCPRAttemptEvent(performer, target, start);
        RaiseLocalEvent(target, ref receiveAttempt);

        if (receiveAttempt.Cancelled)
            return false;

        if (!_hands.TryGetEmptyHand(performer, out _))
            return false;

        return true;
    }

    private bool StartCPR(EntityUid performer, EntityUid target)
    {
        if (!CanCPRPopup(performer, target, true, out _))
            return false;

        var cprComp = EnsureComp<ReceivingCPRComponent>(target);
        cprComp.StartTime = _timing.CurTime;
        Dirty(target, cprComp);

        // If the performer has skills in medical their CPR time will be reduced.
        var delay = TimeSpan.FromSeconds(cprComp.CPRPerformingTime * _skills.GetSkillDelayMultiplier(performer, SkillType));
        var doAfter = new DoAfterArgs(EntityManager, performer, delay, new CPRDoAfterEvent(), performer, target)
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };
        _doAfter.TryStartDoAfter(doAfter);

        if (_net.IsClient)
            return true;

        var selfPopup = Loc.GetString("cm-cpr-self-start-perform", ("target", target));
        _popups.PopupEntity(selfPopup, target, performer, PopupType.Medium);

        var othersPopup = Loc.GetString("cm-cpr-other-start-perform", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.Medium);

        return true;
    }

    private void OnDummyUseInHand(Entity<CPRDummyComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        DeployDummy(ent, args.User);
    }

    private void OnDummyInteractHand(Entity<CPRDummyComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!Transform(ent).Anchored)
        {
            PickupDummy(ent, args.User);
            return;
        }

        StartCPR(args.User, ent);
    }

    private void HandleDummyCPR(Entity<CPRDummyComponent> dummy, EntityUid performer)
    {
        var received = EnsureComp<CPRReceivedComponent>(dummy);
        var tooSoon = received.Last > _timing.CurTime - CPRCooldownSeconds;

        if (tooSoon)
            dummy.Comp.CPRFailed++;
        else
            dummy.Comp.CPRSuccess++;

        Dirty(dummy);

        received.Last = _timing.CurTime;
        Dirty(dummy.Owner, received);

        if (_net.IsClient)
            return;

        if (tooSoon)
        {
            var selfPopup = Loc.GetString("cm-cpr-self-perform-fail-received-too-recently", ("target", dummy));
            _popups.PopupEntity(selfPopup, dummy, performer, PopupType.MediumCaution);

            var othersPopup = Loc.GetString("cm-cpr-other-perform-fail", ("performer", performer), ("target", dummy));
            var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
            _popups.PopupEntity(othersPopup, dummy, othersFilter, true, PopupType.MediumCaution);
        }
        else
        {
            var selfPopup = Loc.GetString("cm-cpr-self-perform", ("target", dummy), ("seconds", (int)CPRCooldownSeconds.TotalSeconds));
            _popups.PopupEntity(selfPopup, dummy, performer, PopupType.Medium);

            var othersPopup = Loc.GetString("cm-cpr-other-perform", ("performer", performer), ("target", dummy));
            var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
            _popups.PopupEntity(othersPopup, dummy, othersFilter, true, PopupType.Medium);
        }
    }

    private void OnDummyExamined(Entity<CPRDummyComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CPRDummyComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-cpr-dummy-examine-successful", ("count", ent.Comp.CPRSuccess)));
            args.PushMarkup(Loc.GetString("rmc-cpr-dummy-examine-failed", ("count", ent.Comp.CPRFailed)));
        }
    }

    private void OnDummyGetAlternativeVerbs(Entity<CPRDummyComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (Transform(ent).Anchored)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-cpr-dummy-verb-pickup"),
                Act = () => PickupDummy(ent, user),
                Priority = 2,
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-cpr-dummy-verb-reset"),
            Act = () => TryResetDummyCounter(ent, user),
            Priority = 1,
        });
    }

    private void DeployDummy(Entity<CPRDummyComponent> ent, EntityUid user)
    {
        var coordinates = _transform.GetMoverCoordinates(user);
        if (!_hands.TryDrop(user, ent))
            return;

        _transform.SetCoordinates(ent, coordinates);
        _transform.AnchorEntity(ent);
        _appearance.SetData(ent, CPRDummyVisuals.Deployed, true);

        if (_net.IsClient)
            return;

        var selfPopup = Loc.GetString("rmc-cpr-dummy-deploy-self");
        _popups.PopupEntity(selfPopup, ent, user, PopupType.Medium);

        var othersPopup = Loc.GetString("rmc-cpr-dummy-deploy-others", ("user", user));
        var othersFilter = Filter.Pvs(user).RemoveWhereAttachedEntity(e => e == user);
        _popups.PopupEntity(othersPopup, ent, othersFilter, true, PopupType.Medium);
    }

    private void PickupDummy(Entity<CPRDummyComponent> ent, EntityUid user)
    {
        _transform.Unanchor(ent);
        _appearance.SetData(ent, CPRDummyVisuals.Deployed, false);

        if (!_hands.TryPickupAnyHand(user, ent))
        {
            _transform.AnchorEntity(ent);
            _appearance.SetData(ent, CPRDummyVisuals.Deployed, true);
        }
    }

    private void TryResetDummyCounter(Entity<CPRDummyComponent> ent, EntityUid user)
    {
        _mind.TryGetMind(user, out var mindId, out _);
        if (!_job.MindTryGetJobId(mindId, out var jobId) || !ent.Comp.ResetCPRCounterJobs.Contains(jobId!.Value))
        {
            if (_net.IsClient)
                return;

            var jobNames = ent.Comp.ResetCPRCounterJobs
                .Select(job => _prototypes.Index(job).LocalizedName)
                .ToList();
            var jobsString = string.Join("; ", jobNames);
            _popups.PopupEntity(Loc.GetString("rmc-cpr-dummy-reset-denied", ("jobs", jobsString)), ent, user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.CPRSuccess = 0;
        ent.Comp.CPRFailed = 0;
        Dirty(ent);

        if (_net.IsClient)
            return;

        _popups.PopupEntity(Loc.GetString("rmc-cpr-dummy-reset-success"), ent, user, PopupType.Medium);
    }
}
