using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CPR;

public sealed class CPRSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    // TODO RMC14 move this to a component
    [ValidatePrototypeId<DamageTypePrototype>]
    private const string HealType = "Asphyxiation";

    private static readonly FixedPoint2 HealAmount = FixedPoint2.New(10);

    public override void Initialize()
    {
        base.Initialize();

        // TODO RMC14 use skills
        // TODO RMC14 something more generic than "marine"
        SubscribeLocalEvent<MarineComponent, InteractHandEvent>(OnMarineInteractHand,
            before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<MarineComponent, CPRDoAfterEvent>(OnMarineDoAfter);

        SubscribeLocalEvent<ReceivingCPRComponent, ReceiveCPRAttemptEvent>(OnReceivingCPRAttempt);
        SubscribeLocalEvent<CPRReceivedComponent, ReceiveCPRAttemptEvent>(OnReceivedCPRAttempt);
        SubscribeLocalEvent<MobStateComponent, ReceiveCPRAttemptEvent>(OnMobStateCPRAttempt);
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

        _unrevivable.AddRevivableTime(target, TimeSpan.FromSeconds(7));

        if (!TryComp(target, out DamageableComponent? damageable) ||
            !damageable.Damage.DamageDict.TryGetValue(HealType, out damage))
        {
            return;
        }

        var heal = -FixedPoint2.Min(damage, HealAmount);
        var healSpecifier = new DamageSpecifier();
        healSpecifier.DamageDict.Add(HealType, heal);
        _damageable.TryChangeDamage(target, healSpecifier, true);
        EnsureComp<CPRReceivedComponent>(target).Last = _timing.CurTime;

        if (_net.IsClient)
            return;

        // TODO RMC14 move this value to a component
        var selfPopup = Loc.GetString("cm-cpr-self-perform", ("target", target), ("seconds", 7));
        _popups.PopupEntity(selfPopup, target, performer, PopupType.Medium);

        var othersPopup = Loc.GetString("cm-cpr-other-perform", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.Medium);
    }

    private void OnReceivingCPRAttempt(Entity<ReceivingCPRComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
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

        // TODO RMC14 move this value to a component
        if (ent.Comp.Last > _timing.CurTime - TimeSpan.FromSeconds(7))
        {
            args.Cancelled = true;

            if (_net.IsClient)
                return;

            var selfPopup = Loc.GetString("cm-cpr-self-perform-fail-received-too-recently", ("target", target));
            _popups.PopupEntity(selfPopup, target, performer, PopupType.MediumCaution);

            var othersPopup = Loc.GetString("cm-cpr-other-perform-fail", ("performer", performer), ("target", target));
            var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
            _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.MediumCaution);
        }
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

        if (!HasComp<MarineComponent>(target) || !HasComp<MarineComponent>(performer))
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
}
