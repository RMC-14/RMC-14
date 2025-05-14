using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tackle;

public sealed class TackleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCHandsSystem _rmcHands = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    private readonly List<EntityUid> _trackersToRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<TackleableComponent, CMDisarmEvent>(OnDisarmed, before: [typeof(SharedHandsSystem), typeof(StaminaSystem)]);
        SubscribeLocalEvent<RMCDisarmableComponent, CMDisarmEvent>(OnDisarmed, before: [typeof(SharedHandsSystem), typeof(StaminaSystem)]);

        SubscribeLocalEvent<TackledRecentlyByComponent, ComponentRemove>(OnByRemove);
        SubscribeLocalEvent<TackledRecentlyByComponent, EntityTerminatingEvent>(OnByRemove);
        SubscribeLocalEvent<TackledRecentlyByComponent, DownedEvent>(OnDowned);

        SubscribeLocalEvent<TackledRecentlyComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<TackledRecentlyComponent, EntityTerminatingEvent>(OnRemove);
    }

    private void OnDisarmed(Entity<TackleableComponent> target, ref CMDisarmEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out TackleComponent? tackle))
            return;

        args.Handled = true;

        DoDisarmEffects(user, target);

        var time = _timing.CurTime;
        var recently = EnsureComp<TackledRecentlyComponent>(user);
        var tracker = recently.Trackers.GetValueOrDefault(target);
        tracker.Count++;
        tracker.Last = time;

        recently.Trackers[target] = tracker;
        Dirty(user, recently);

        var tackledBy = EnsureComp<TackledRecentlyByComponent>(target);
        tackledBy.Tacklers.Add(user);
        Dirty(target, tackledBy);

        if (_net.IsClient)
            return;

        if (tracker.Count < tackle.Min ||
            tracker.Count < _random.Next(tackle.Min, tackle.Max + 1))
        {
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tried to tackle {ToPrettyString(target)}.");

            var selfPopup = Loc.GetString("cm-tackle-try-self", ("target", target.Owner));
            var othersPopup = Loc.GetString("cm-tackle-try-observer", ("user", user), ("target", target.Owner));
            var targetPopup = Loc.GetString("cm-tackle-try-target", ("user", user));
            DoPvsPopups(user, target, selfPopup, othersPopup, targetPopup);

            return;
        }
        else
        {
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tackled down {ToPrettyString(target)}.");

            var selfPopup = Loc.GetString("cm-tackle-success-self", ("target", target.Owner));
            var othersPopup = Loc.GetString("cm-tackle-success-observer", ("user", user), ("target", target.Owner));
            var targetPopup = Loc.GetString("cm-tackle-success-target", ("user", user));
            DoPvsPopups(user, target, selfPopup, othersPopup, targetPopup);
        }

        _audio.PlayPvs(target.Comp.KnockdownSound, target);

        if (!HasComp<VictimInfectedComponent>(target))
        {
            recently.Trackers.Remove(target);
            RemoveTackledBy(target.Owner, user);
        }

        var stun = tackle.StunMin;
        if (tackle.StunMin < tackle.StunMax)
            stun = _random.Next(tackle.StunMin, tackle.StunMax);

        stun *= 2;
        _stun.TryParalyze(target, stun, true);
    }

    private void OnDisarmed(Entity<RMCDisarmableComponent> target, ref CMDisarmEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out RMCDisarmComponent? disarm))
            return;

        args.Handled = true;

        DoDisarmEffects(user, target);

        if (_net.IsClient)
            return;

        var doPopups = true;

        if (!_skills.HasSkill(user, disarm.Skill, disarm.AccidentalDischargeSkillAmount))
        {
            var fired = false;

            foreach (var item in _hands.EnumerateHeld(target))
            {
                if (fired)
                    break;

                if (TryComp<GunComponent>(item, out var gun))
                {
                    if (_random.Prob(disarm.AccidentalDischargeChance))
                    {
                        var coords = _transform.GetMoverCoordinates(user);
                        var shotProjectiles = _gunSystem.AttemptShoot((item, gun), target, coords);

                        var ammoCount = new GetAmmoCountEvent();
                        RaiseLocalEvent(item, ref ammoCount);

                        if (shotProjectiles != null && ammoCount.Count > 0)
                        {
                            fired = true;
                            doPopups = false; // Disable other popups if the gun was discharged so we dont get stacked popups

                            var selfMsg = Loc.GetString("rmc-disarm-discharge-self", ("targetName", target), ("gun", item));
                            var othersMsg = Loc.GetString("rmc-disarm-discharge-others", ("performerName", user), ("targetName", target), ("gun", item));
                            var targetMsg = Loc.GetString("rmc-disarm-discharge-target", ("performerName", user), ("gun", item));
                            DoPvsPopups(user, target, selfMsg, othersMsg, targetMsg, PopupType.MediumCaution);

                            var ev = new UpdateClientAmmoEvent();
                            RaiseLocalEvent(item, ref ev);
                        }
                    }
                }
            }

            if (fired)
                _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} accidentally discharged {ToPrettyString(target)}'s gun while trying to disarm them.");
        }

        var disarmChance = _random.NextFloat(1, 100);
        var attackerSkill = _skills.GetSkill(user, disarm.Skill);
        var defenderSkill = _skills.GetSkill(target.Owner, disarm.Skill);
        disarmChance -= 5 * attackerSkill;
        disarmChance += 5 * defenderSkill;

        if (disarmChance <= 25)
        {
            var shoveText = Loc.GetString(attackerSkill > 1 ? "rmc-disarm-text-skilled" : _random.Pick(disarm.RandomShoveTexts));

            if (doPopups)
            {
                var selfMsg = Loc.GetString("rmc-disarm-shove-self", ("targetName", target), ("shoveText", shoveText));
                var othersMsg = Loc.GetString("rmc-disarm-shove-others", ("performerName", user), ("targetName", target), ("shoveText", shoveText));
                var targetMsg = Loc.GetString("rmc-disarm-shove-target", ("performerName", user), ("shoveText", shoveText));
                DoPvsPopups(user, target, selfMsg, othersMsg, targetMsg);
            }

            var strength = disarm.BaseStunTime + TimeSpan.FromSeconds(Math.Max(attackerSkill - defenderSkill, 0));
            _stun.TryParalyze(target, strength, true);
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} disarmed {ToPrettyString(target)}, stunning them.");
            return;
        }

        if (disarmChance <= 60)
        {
            if (TryComp<PullerComponent>(target, out var puller) && puller.Pulling is { } pulledObject)
            {
                if (doPopups)
                {
                    var selfMsg = Loc.GetString("rmc-disarm-break-pulls-self", ("targetName", target), ("object", pulledObject));
                    var othersMsg = Loc.GetString("rmc-disarm-break-pulls-others", ("performerName", user), ("targetName", target), ("object", pulledObject));
                    var targetMsg = Loc.GetString("rmc-disarm-break-pulls-target", ("performerName", user), ("object", pulledObject));
                    DoPvsPopups(user, target, selfMsg, othersMsg, targetMsg);
                }

                _rmcPulling.TryStopAllPullsFromAndOn(target);
            }
            else
            {
                if (doPopups)
                {
                    var selfMsg = Loc.GetString("rmc-disarm-success-self", ("targetName", target));
                    var othersMsg = Loc.GetString("rmc-disarm-success-others", ("performerName", user), ("targetName", target));
                    var targetMsg = Loc.GetString("rmc-disarm-success-target", ("performerName", user));
                    DoPvsPopups(user, target, selfMsg, othersMsg, targetMsg);
                }

                var offset = _transform.GetMoverCoordinates(target).Offset(_random.NextVector2(1f, 1.5f));
                _rmcHands.ThrowHeldItem(target, offset);
            }

            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} disarmed {ToPrettyString(target)}.");

            return;
        }

        _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tried to disarm {ToPrettyString(target)}.");

        if (!doPopups)
            return;

        // Disarm failed
        var selfPopup = Loc.GetString("rmc-disarm-attempt-self", ("targetName", target));
        var othersPopup = Loc.GetString("rmc-disarm-attempt-others", ("performerName", user), ("targetName", target));
        var targetPopup = Loc.GetString("rmc-disarm-attempt-target", ("performerName", user));
        DoPvsPopups(user, target, selfPopup, othersPopup, targetPopup);
    }

    public void DoDisarmEffects(EntityUid user, EntityUid target)
    {
        _colorFlash.RaiseEffect(Color.Aqua, new List<EntityUid> { target }, Filter.PvsExcept(user));
    }

    public void DoPvsPopups(EntityUid user, EntityUid target, string selfPopup, string othersPopup, string targetPopup, PopupType selfPopupType = PopupType.Small)
    {
        _popup.PopupEntity(selfPopup, user, user, selfPopupType);

        foreach (var session in Filter.PvsExcept(user).Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            if (recipient == target)
                _popup.PopupEntity(targetPopup, user, recipient, PopupType.MediumCaution);
            else
                _popup.PopupEntity(othersPopup, user, recipient, PopupType.SmallCaution);
        }
    }

    private void OnByRemove<T>(Entity<TackledRecentlyByComponent> ent, ref T args)
    {
        foreach (var tackler in ent.Comp.Tacklers)
        {
            if (!TryComp(tackler, out TackledRecentlyComponent? recently))
                continue;

            recently.Trackers.Remove(ent);
            Dirty(tackler, recently);
        }
    }

    private void OnDowned(Entity<TackledRecentlyByComponent> ent, ref DownedEvent args)
    {
        if (!HasComp<VictimInfectedComponent>(ent) && (!TryComp<BuckleComponent>(ent, out var buckle) || !buckle.Buckled))
            RemCompDeferred<TackledRecentlyByComponent>(ent);
    }

    private void OnRemove<T>(Entity<TackledRecentlyComponent> ent, ref T args)
    {
        foreach (var tracker in ent.Comp.Trackers)
        {
            if (!TryComp(tracker.Key, out TackledRecentlyByComponent? tackled))
                continue;

            tackled.Tacklers.Remove(ent);
        }
    }

    private void RemoveTackledBy(Entity<TackledRecentlyByComponent?> by, EntityUid tackler)
    {
        if (!Resolve(by, ref by.Comp, false))
            return;

        by.Comp.Tacklers.Remove(tackler);
        Dirty(by);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<TackledRecentlyComponent>();
        while (query.MoveNext(out var tackler, out var recently))
        {
            _trackersToRemove.Clear();
            foreach (var tracker in recently.Trackers)
            {
                if (time >= tracker.Value.Last + recently.ExpireAfter)
                    _trackersToRemove.Add(tracker.Key);
            }

            foreach (var id in _trackersToRemove)
            {
                recently.Trackers.Remove(id);
                RemoveTackledBy(id, tackler);
            }

            if (recently.Trackers.Count == 0)
                RemCompDeferred<TackledRecentlyComponent>(tackler);
        }
    }
}
