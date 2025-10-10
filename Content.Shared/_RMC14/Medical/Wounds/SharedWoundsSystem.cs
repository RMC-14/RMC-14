using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Wounds;

public abstract class SharedWoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _bloodlossMultiplier = 1;
    private float _bleedTimeMultiplier = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, CMBleedAttemptEvent>(OnMobStateBleedAttempt);

        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnWoundableDamaged);
        SubscribeLocalEvent<WoundableComponent, CMBleedEvent>(OnWoundableBleed);

        // TODO RMC14 anti-bleed medicines
        SubscribeLocalEvent<WoundedComponent, RejuvenateEvent>(OnWoundedRejuvenate);
        SubscribeLocalEvent<WoundedComponent, EntityUnpausedEvent>(OnWoundedUnpaused);

        SubscribeLocalEvent<WoundTreaterComponent, UseInHandEvent>(OnWoundTreaterUseInHand);
        SubscribeLocalEvent<WoundTreaterComponent, AfterInteractEvent>(OnWoundTreaterAfterInteract);
        SubscribeLocalEvent<WoundTreaterComponent, TreatWoundDoAfterEvent>(OnWoundTreaterDoAfter);

        Subs.CVar(_config, RMCCVars.CMBloodlossMultiplier, v => _bloodlossMultiplier = v, true);
        Subs.CVar(_config, RMCCVars.CMBleedTimeMultiplier, v => _bleedTimeMultiplier = v, true);
    }

    private void OnMobStateBleedAttempt(Entity<MobStateComponent> ent, ref CMBleedAttemptEvent args)
    {
        if (ent.Comp.CurrentState == MobState.Dead)
            args.Cancelled = true;
    }

    private void OnWoundableDamaged(Entity<WoundableComponent> ent, ref DamageChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.DamageDelta == null)
            return;

        FixedPoint2? limit = HasComp<WoundTreaterComponent>(args.Tool)
            ? FixedPoint2.New(0.5f)
            : null;

        TryHealWounds((ent.Owner, args.Damageable), args.DamageDelta, limit);

        if (args.DamageIncreased)
        {
            TryAddWound(ent, ent.Comp.BruteWoundGroup, args.DamageDelta, WoundType.Brute);
            TryAddWound(ent, ent.Comp.BurnWoundGroup, args.DamageDelta, WoundType.Burn);
        }
    }

    private void OnWoundableBleed(Entity<WoundableComponent> ent, ref CMBleedEvent args)
    {
        args.Handled = true;
    }

    private void OnWoundedRejuvenate(Entity<WoundedComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<WoundedComponent>(ent);
    }

    private void OnWoundedUnpaused(Entity<WoundedComponent> ent, ref EntityUnpausedEvent args)
    {
        var wounds = CollectionsMarshal.AsSpan(ent.Comp.Wounds);
        foreach (ref var wound in wounds)
        {
            if (wound.StopBleedAt != null)
                wound.StopBleedAt = wound.StopBleedAt + args.PausedTime;
        }

        ent.Comp.UpdateAt += args.PausedTime;
        Dirty(ent);
    }

    private void OnWoundTreaterUseInHand(Entity<WoundTreaterComponent> ent, ref UseInHandEvent args)
    {
        StartTreatment(args.User, args.User, ent, out var handled);
        args.Handled = handled;
    }

    private void OnWoundTreaterAfterInteract(Entity<WoundTreaterComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        StartTreatment(args.User, args.Target.Value, ent, out var handled);
        args.Handled = handled;
    }

    private void OnWoundTreaterDoAfter(Entity<WoundTreaterComponent> treater, ref TreatWoundDoAfterEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Cancelled ||
            args.Target is not { } target ||
            !TryComp(target, out DamageableComponent? damageable))
        {
            return;
        }

        if (!CanTreatPopup(user, target, treater, out var wounded, out var damage, out var handled))
        {
            args.Handled = handled;
            return;
        }

        args.Handled = true;
        if (damage != FixedPoint2.Zero)
        {
            var total = _rmcDamageable.DistributeDamageCached((target, damageable), treater.Comp.Group, damage);
            _damageable.TryChangeDamage(target, total, true, damageable: damageable, origin: user, tool: args.Used);
        }

        var anyWounds = false;
        var wounds = CollectionsMarshal.AsSpan(wounded.Wounds);
        foreach (ref var wound in wounds)
        {
            if (wound.Type != treater.Comp.Wound || wound.Treated)
                continue;

            if (!treater.Comp.Treats && FixedPoint2.Abs(wound.Healed) < wound.Damage / 2)
                continue;

            wound.Treated = true;
            anyWounds = true;
        }

        if (anyWounds)
        {
            Dirty(target, wounded);
        }
        else if (damage == FixedPoint2.Zero)
        {
            if (user == target)
            {
                if (treater.Comp.NoWoundsOnUserPopup is { } popup)
                    _popup.PopupClient(Loc.GetString(popup), user, user);
            }
            else if (treater.Comp.NoWoundsOnTargetPopup is { } popup)
            {
                _popup.PopupClient(Loc.GetString(popup), user, user);
            }

            return;
        }

        _audio.PlayPredicted(treater.Comp.TreatEndSound, user, user);

        if (treater.Comp.Consumable)
        {
            if (TryComp(treater, out StackComponent? stack))
                _stacks.Use(treater, 2, stack);
            else if (_net.IsServer)
                QueueDel(treater);
        }
        else if (CanTreatPopup(user, target, treater, out _, out _, out _, false))
        {
            args.Repeat = true;
        }

        var userPopup = treater.Comp.UserPopup;
        var targetPopup = treater.Comp.TargetPopup;
        var othersPopup = treater.Comp.OthersPopup;
        if (!args.Repeat)
        {
            if (treater.Comp.UserFinishPopup != null)
                userPopup = treater.Comp.UserFinishPopup;

            if (treater.Comp.TargetFinishPopup != null)
                targetPopup = treater.Comp.TargetFinishPopup;

            if (treater.Comp.OthersFinishPopup != null)
                othersPopup = treater.Comp.OthersFinishPopup;
        }

        if (userPopup != null)
            _popup.PopupClient(Loc.GetString(userPopup, ("target", target)), target, user);

        if (user != target && targetPopup != null)
            _popup.PopupEntity(Loc.GetString(targetPopup, ("user", user)), target, target);

        if (user != target && othersPopup != null)
        {
            var others = Filter.PvsExcept(target).RemoveWhereAttachedEntity(e => e == user || e == target);
            _popup.PopupEntity(Loc.GetString(othersPopup, ("user", user), ("target", target)), user, others, true);
        }
    }

    private bool CanTreatPopup(EntityUid user,
        EntityUid target,
        Entity<WoundTreaterComponent> treater,
        [NotNullWhen(true)] out WoundedComponent? wounded,
        out FixedPoint2 damage,
        out bool handle,
        bool doPopups = true)
    {
        handle = false;
        wounded = default;
        damage = FixedPoint2.Zero;
        if (!HasComp<WoundableComponent>(target) &&
            !TryComp(target, out wounded))
        {
            return false;
        }

        if (HasComp<WoundableUntreatableComponent>(target))
            return false;

        var targetName = Identity.Name(target, EntityManager, user);
        var hasSkills = _skills.HasAllSkills(user, treater.Comp.Skills);
        if (!treater.Comp.CanUseUnskilled && !hasSkills)
        {
            if (doPopups)
                _popup.PopupClient(Loc.GetString("cm-wounds-failed-unskilled", ("treater", treater.Owner)), target, user, PopupType.SmallCaution);

            return false;
        }

        if (!TryComp(target, out wounded) ||
            wounded.Wounds.Count == 0)
        {
            if (user == target)
            {
                if (doPopups && treater.Comp.NoneSelfPopup is { } selfPopup)
                    _popup.PopupClient(Loc.GetString(selfPopup), target, user);

                return false;
            }

            if (doPopups && treater.Comp.NoneOtherPopup is { } otherPopup)
                _popup.PopupClient(Loc.GetString(otherPopup, ("target", target)), target, user);

            return false;
        }

        var treaterDamage = hasSkills ? treater.Comp.Damage : treater.Comp.UnskilledDamage;
        var max = treaterDamage ?? FixedPoint2.Zero;
        var untreated = false;
        var surgeryUntreated = false;
        var otherUntreated = false;
        var divisor = FixedPoint2.New(2);
        var wounds = CollectionsMarshal.AsSpan(wounded.Wounds);
        foreach (ref var wound in wounds)
        {
            if (wound.Type == WoundType.Surgery &&
                treater.Comp.Wound != WoundType.Surgery &&
                !wound.Treated)
            {
                surgeryUntreated = true;
            }

            if (wound.Type != treater.Comp.Wound && !wound.Treated)
                otherUntreated = true;

            if (wound.Type != treater.Comp.Wound)
                continue;

            if (treater.Comp.Treats && wound.Treated)
                continue;

            if (max == FixedPoint2.Zero)
            {
                if (!wound.Treated)
                    untreated = true;

                continue;
            }

            var limit = wound.Damage / divisor;
            if (FixedPoint2.Abs(wound.Healed) < limit)
                damage += -FixedPoint2.Min(limit - wound.Healed, FixedPoint2.Abs(max - damage));

            if (damage <= max)
                break;

            if (!wound.Treated)
                untreated = true;
        }

        if (untreated || damage != FixedPoint2.Zero)
        {
            if (treater.Comp.Consumable &&
                TryComp(treater, out StackComponent? stack) &&
                _stacks.GetCount(treater, stack) < 2)
            {
                _popup.PopupClient(Loc.GetString("cm-wounds-failed-not-enough", ("treater", treater.Owner)), target, user, PopupType.SmallCaution);
                return false;
            }

            return true;
        }

        if (doPopups)
        {
            if (surgeryUntreated)
                _popup.PopupClient(Loc.GetString("cm-wounds-open-cut", ("target", targetName), ("treater", treater.Owner)), target, user, PopupType.SmallCaution);
            else if (otherUntreated)
                _popup.PopupClient(Loc.GetString("cm-wounds-cannot-treat", ("treater", treater.Owner)), target, user, PopupType.SmallCaution);
            else
                _popup.PopupClient(Loc.GetString("cm-wounds-already-treated", ("target", target)), target, user);
        }

        wounded = default;
        return false;
    }

    private void StartTreatment(EntityUid user, EntityUid target, Entity<WoundTreaterComponent> treater, out bool handled)
    {
        handled = false;
        if (!CanTreatPopup(user, target, treater, out _, out var damage, out handled))
            return;

        handled = true;
        var delay = _skills.GetDelay(user, treater);
        if (delay > TimeSpan.Zero)
            _popup.PopupClient(Loc.GetString("cm-wounds-start-fumbling", ("name", treater.Owner)), target, user);

        var scaling = treater.Comp.ScalingDoAfter;
        scaling *= _skills.GetSkillDelayMultiplier(user, treater.Comp.DoAfterSkill, treater.Comp.DoAfterSkillMultipliers);
        if (user == target)
            scaling *= treater.Comp.SelfTargetDoAfterMultiplier;

        if (scaling > TimeSpan.Zero)
        {
            var scaledDoAfter = scaling * damage.Double();

            var minimumDoAfter = treater.Comp.MinimumDoAfter;
            if (scaledDoAfter < minimumDoAfter)
                scaledDoAfter = minimumDoAfter;

            if (scaledDoAfter > TimeSpan.Zero)
            {
                delay += scaledDoAfter;
            }
        }

        if (user != target && treater.Comp.TargetStartPopup != null)
            _popup.PopupEntity(Loc.GetString(treater.Comp.TargetStartPopup, ("user", user)), target, target);

        var ev = new TreatWoundDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, treater, target, treater)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
            MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doAfter);
        _audio.PlayPredicted(treater.Comp.TreatBeginSound, user, user);
    }

    private void TryAddWound(Entity<WoundableComponent> woundable,
        ProtoId<DamageGroupPrototype> groupId,
        DamageSpecifier damage,
        WoundType type)
    {
        if (!_prototypes.TryIndex(groupId, out var group) ||
            !damage.TryGetDamageInGroup(group, out var total) ||
            total <= FixedPoint2.Zero)
        {
            return;
        }

        AddWound((woundable, woundable), total, type);
    }

    public void TryHealWounds(Entity<DamageableComponent, WoundedComponent?> wounded, DamageSpecifier damage, FixedPoint2? limit = null)
    {
        var (uid, damageable, comp) = wounded;
        if (!Resolve(wounded, ref comp, false))
            return;

        if (comp.Wounds.Count == 0)
            return;

        HealOrRemove((uid, damageable, comp), comp.BruteWoundGroup, WoundType.Brute, damage, limit);
        HealOrRemove((uid, damageable, comp), comp.BurnWoundGroup, WoundType.Burn, damage, limit);
    }

    private void HealOrRemove(Entity<DamageableComponent, WoundedComponent> wounded,
        ProtoId<DamageGroupPrototype> group,
        WoundType type,
        DamageSpecifier damage,
        FixedPoint2? limit = null)
    {
        if (wounded.Comp1.DamagePerGroup.GetValueOrDefault(group) <= FixedPoint2.Zero)
        {
            RemoveWounds((wounded, wounded), type);
        }
        else if (_prototypes.TryIndex(group, out var bruteGroup) &&
                 damage.TryGetDamageInGroup(bruteGroup, out var bruteDelta))
        {
            TryHealWounds((wounded, wounded), type, bruteDelta, limit);
        }
    }

    public void TryHealWounds(Entity<WoundedComponent?> wounded,
        WoundType type,
        FixedPoint2 amount,
        FixedPoint2? limit = null)
    {
        if (amount >= FixedPoint2.Zero)
            return;

        if (!Resolve(wounded, ref wounded.Comp, false) ||
            wounded.Comp.Wounds.Count == 0)
        {
            return;
        }

        limit ??= FixedPoint2.New(1f);
        var wounds = CollectionsMarshal.AsSpan(wounded.Comp.Wounds);
        for (var i = 0; i < wounds.Length; i++)
        {
            ref var wound = ref wounds[i];
            if (wound.Type != type)
                continue;

            var healing = -FixedPoint2.Max(-(wound.Damage * limit.Value - wound.Healed), amount);
            if (healing == FixedPoint2.Zero)
                continue;

            wound.Healed += healing;
            amount += healing;

            if (amount == FixedPoint2.Zero)
                break;
        }
    }

    public void AddWound(Entity<WoundableComponent?> woundable, FixedPoint2 total, WoundType type, TimeSpan? fixedDuration = null)
    {
        if (!Resolve(woundable, ref woundable.Comp, false))
            return;

        var bloodloss = 0f;
        if (type != WoundType.Burn &&
            total >= woundable.Comp.BleedMinDamage)
        {
            bloodloss = total.Float() * woundable.Comp.BloodLossMultiplier;
        }

        bloodloss *= _bloodlossMultiplier;

        var time = _timing.CurTime;
        var duration = fixedDuration ?? total.Float() * woundable.Comp.DurationMultiplier * _bleedTimeMultiplier;
        if (EnsureComp<WoundedComponent>(woundable, out var wounded))
        {
            var wounds = CollectionsMarshal.AsSpan(wounded.Wounds);
            for (var i = wounds.Length - 1; i >= 0; i--)
            {
                ref var wound = ref wounds[i];
                if (wound.Type != type)
                    continue;

                if (wound.StopBleedAt is not { } stopBleedAt || time >= stopBleedAt)
                    continue;

                if (wound.Bloodloss > 0)
                {
                    wound.Bloodloss += bloodloss / 1.5f;
                    wound.StopBleedAt = stopBleedAt + duration / 1.5f;
                    bloodloss = 0;
                    duration = TimeSpan.Zero;
                    break;
                }
            }
        }

        wounded.BruteWoundGroup = woundable.Comp.BruteWoundGroup;
        wounded.BurnWoundGroup = woundable.Comp.BurnWoundGroup;

        TimeSpan? newDuration = duration == TimeSpan.MaxValue ? null : time + duration;
        wounded.Wounds.Add(new Wound(total, FixedPoint2.Zero, bloodloss, newDuration, type, false));
        Dirty(woundable, wounded);
    }

    public void RemoveWounds(Entity<WoundedComponent?> wounded, WoundType type)
    {
        if (!Resolve(wounded, ref wounded.Comp, false))
            return;

        var wounds = wounded.Comp.Wounds;
        for (var i = wounds.Count - 1; i >= 0; i--)
        {
            if (wounds[i].Type == type)
                wounds.RemoveSwap(i);
        }
    }

    public bool HasUntreated(Entity<WoundedComponent?> wounded, ProtoId<DamageGroupPrototype> group)
    {
        if (!Resolve(wounded, ref wounded.Comp, false) ||
            wounded.Comp.Wounds.Count == 0)
        {
            return false;
        }

        WoundType type;
        if (group == wounded.Comp.BruteWoundGroup)
            type = WoundType.Brute;
        else if (group == wounded.Comp.BurnWoundGroup)
            type = WoundType.Burn;
        else
            return false;

        var wounds = CollectionsMarshal.AsSpan(wounded.Comp.Wounds);
        foreach (ref var wound in wounds)
        {
            if (wound.Type == type && !wound.Treated)
                return true;
        }

        return false;
    }
}
