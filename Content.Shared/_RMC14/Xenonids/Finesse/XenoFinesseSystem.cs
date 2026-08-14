using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Impale;
using Content.Shared._RMC14.Xenonids.TailTrip;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.Finesse;

public sealed class XenoFinesseSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MarineComponent>> _marines = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoFinesseComponent, MeleeHitEvent>(OnFinesseMeleeHit);

        SubscribeLocalEvent<XenoSpreadMarkAttemptComponent, DamageChangedEvent>(OnXenoMarkedDamageChanged, after: [typeof(MobThresholdSystem)]);
    }

    private void OnFinesseMeleeHit(Entity<XenoFinesseComponent> xeno, ref MeleeHitEvent args)
    {
        var time = _timing.CurTime;

        foreach (var ent in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            var comp = AddMarkedTag(xeno, ent, time);

            if (comp.IsCriticalTag)
            {
                //Consume it
                comp.IsCriticalTag = false;
                _popup.PopupEntity(Loc.GetString("rmc-xeno-marked-critical-consumed"), ent, ent, PopupType.SmallCaution);
                Dirty(ent, comp);

                RemoveCooldowns(xeno);
            }

            //Use a seperate component to see if tags spread
            //As damage hasn't been done yet
            var spreadAttempt = EnsureComp<XenoSpreadMarkAttemptComponent>(ent);
            spreadAttempt.Origin = xeno;
            spreadAttempt.TimeOfSpreadAttempt = time;
            Dirty(ent, spreadAttempt);

            return;
        }
    }

    private void OnXenoMarkedDamageChanged(Entity<XenoSpreadMarkAttemptComponent> marked, ref DamageChangedEvent args)
    {
        if (!TryComp<XenoFinesseComponent>(marked.Comp.Origin, out var finesse))
            return;

        TrySpreadTagsFrom((marked.Comp.Origin, finesse), marked, marked.Comp.TimeOfSpreadAttempt);
        RemCompDeferred<XenoSpreadMarkAttemptComponent>(marked);
    }

    private void TrySpreadTagsFrom(Entity<XenoFinesseComponent> xeno, EntityUid hit, TimeSpan currTime)
    {
        if (_mob.IsAlive(hit))
            return;

        if (!HasComp<MarineComponent>(hit))
            return;

        if (currTime < xeno.Comp.NextCriticalMarkSpreadTime)
            return;

        if (HasComp<XenoCriticalMarkSpreadImmunityComponent>(hit))
            return;

        EnsureComp<XenoCriticalMarkSpreadImmunityComponent>(hit).WearOffAt = currTime + xeno.Comp.CriticalMarkSpreadImmuneDuration;

        var spreadEffected = 0;
        var selfCoords = xeno.Owner.ToCoordinates();

        _marines.Clear();
        _entityLookup.GetEntitiesInRange(selfCoords, xeno.Comp.SpreadCriticalMarkRange, _marines);

        var nearestMarines = _marines.ToList().OrderBy(a => selfCoords.TryDistance(EntityManager, a.Owner.ToCoordinates(), out var distance) ? distance : 20).ToList();

        foreach (var marine in nearestMarines)
        {
            if (!_examine.InRangeUnOccluded(xeno, marine, xeno.Comp.SpreadCriticalMarkRange))
                continue;

            if (marine.Owner == hit)
                continue;

            if (!_xeno.CanAbilityAttackTarget(xeno, marine) ||
                _mob.IsCritical(marine) || _status.HasStatusEffect(marine, "KnockedDown"))
                continue;

            if (HasComp<XenoMarkedComponent>(marine))
                continue;

            var comp = AddMarkedTag(xeno, marine, currTime, true);

            spreadEffected++;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-marked-critical-apply"), marine, marine, PopupType.MediumCaution);

            if (xeno.Comp.MaxCriticalMarkSpread != null && spreadEffected >= xeno.Comp.MaxCriticalMarkSpread)
                break;
        }

        if (spreadEffected > 0)
        {
            xeno.Comp.NextCriticalMarkSpreadTime = currTime + xeno.Comp.CritcalMarkSpreadCooldown;
            Dirty(xeno);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var markedQuery = EntityQueryEnumerator<XenoMarkedComponent>();

        while (markedQuery.MoveNext(out var uid, out var mark))
        {
            if (time < mark.WearOffAt)
                continue;

            if (mark.IsCriticalTag)
                _popup.PopupEntity(Loc.GetString("rmc-xeno-marked-critical-disappear"), uid, uid, PopupType.SmallCaution);

            RemCompDeferred<XenoMarkedComponent>(uid);
        }

        var immuneQuery = EntityQueryEnumerator<XenoCriticalMarkSpreadImmunityComponent>();

        while (immuneQuery.MoveNext(out var uid, out var immunity))
        {
            if (time < immunity.WearOffAt)
                continue;

            RemCompDeferred<XenoCriticalMarkSpreadImmunityComponent>(uid);
        }
    }

    private XenoMarkedComponent AddMarkedTag(Entity<XenoFinesseComponent> xeno, EntityUid target, TimeSpan applyTime, bool criticalMark = false)
    {
        var mark = EnsureComp<XenoMarkedComponent>(target);

        mark.WearOffAt = applyTime + (criticalMark ? xeno.Comp.CriticalMarkTime : xeno.Comp.MarkedTime);
        mark.TimeAdded = applyTime;
        if (criticalMark)
            mark.IsCriticalTag = true;

        Dirty(target, mark);

        return mark;
    }

    private void RemoveCooldowns(Entity<XenoFinesseComponent> xeno)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            var actionEvent = _actions.GetEvent(action);

            if ((actionEvent is XenoImpaleActionEvent || actionEvent is XenoTailTripActionEvent)
                && action.Comp.Cooldown != null)
            {
                _actions.ClearCooldown(action.AsNullable());
            }
        }
    }
}
