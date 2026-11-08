using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Rules;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Server.Temperature.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private bool UpdateScuttle(CMDistressSignalRuleComponent rule, float frameTime)
    {
        if (rule.ScuttleDetonated)
            return UpdateScuttleRoundEnd(rule);

        if (rule.Result != null)
            return false;

        var time = Timing.CurTime;
        if (!rule.ScuttleUnlocked &&
            rule.ScuttleUnlockAt is { } unlockAt &&
            time >= unlockAt)
        {
            UnlockScuttle(rule);
        }

        if (!rule.ScuttleUnlocked)
            return false;

        if (rule.ScuttleFinalSequenceStarted)
            return UpdateScuttleFinalSequence(rule);

        var overloaded = CountOverloadedScuttleReactors();
        if (overloaded <= 0)
            return false;

        rule.ScuttleProgress += TimeSpan.FromSeconds(frameTime);

        var required = GetScuttleRequiredDuration(rule, overloaded, GetScuttleTotalReactors(rule, overloaded));
        if (!rule.ScuttleOneThirdAnnounced && HasReachedScuttleProgress(rule, required, 1.0 / 3.0))
        {
            rule.ScuttleOneThirdAnnounced = true;
            AnnounceScuttleStage(rule, "rmc-distress-signal-scuttle-stage-one", rule.ScuttleStageFireRange, rule.ScuttleStageFireIntensity, rule.ScuttleStageFireDuration);
        }

        if (!rule.ScuttleHalfwayAnnounced && HasReachedScuttleProgress(rule, required, 0.5))
        {
            rule.ScuttleHalfwayAnnounced = true;
            AnnounceScuttleStage(rule, "rmc-distress-signal-scuttle-halfway", rule.ScuttleStageFireRange, rule.ScuttleStageFireIntensity, rule.ScuttleStageFireDuration);
        }

        if (!rule.ScuttleTwoThirdsAnnounced && HasReachedScuttleProgress(rule, required, 2.0 / 3.0))
        {
            rule.ScuttleTwoThirdsAnnounced = true;
            AnnounceScuttleStage(rule, "rmc-distress-signal-scuttle-stage-two", rule.ScuttleFinalFireRange, rule.ScuttleFinalFireIntensity, rule.ScuttleFinalFireDuration);
        }

        UpdateScuttleHeatAura(rule);

        if (rule.ScuttleProgress >= required)
            StartScuttleFinalSequence(rule);

        return false;
    }

    private void UnlockScuttle(CMDistressSignalRuleComponent rule)
    {
        rule.ScuttleUnlocked = true;
        rule.ScuttleUnlockAt = null;
        rule.ScuttleTotalReactors = CountMappedScuttleReactors();

        _alertLevelSystem.Set(RMCAlertLevels.Delta, null, true, false);
        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-unlocked"),
            announcement: "rmc-announcement-ares-command");
    }

    private void StartScuttleFinalSequence(CMDistressSignalRuleComponent rule)
    {
        if (rule.ScuttleFinalSequenceStarted)
            return;

        var finalStartedAt = Timing.CurTime;
        rule.ScuttleFinalSequenceStarted = true;
        rule.ScuttleFinalStartedAt = finalStartedAt;
        SetScuttleFinalTimes(rule, finalStartedAt);

        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-final"),
            rule.ScuttleStageSound,
            "rmc-announcement-ares-command");

        AnnounceScuttleDeckCreak(rule);
        SpawnScuttleFireAroundOverloadedReactors(rule, rule.ScuttleFinalFireRange, rule.ScuttleFinalFireIntensity, rule.ScuttleFinalFireDuration);
    }

    private bool UpdateScuttleFinalSequence(CMDistressSignalRuleComponent rule)
    {
        var time = Timing.CurTime;
        var startedAt = rule.ScuttleFinalStartedAt ?? time;
        if (rule.ScuttleFinalStartedAt == null)
            rule.ScuttleFinalStartedAt = startedAt;
        if (rule.ScuttleFinalDetonateAt == null || rule.ScuttleRoundEndAt == null)
            SetScuttleFinalTimes(rule, startedAt);

        var elapsed = time - startedAt;
        if (!rule.ScuttleFinalMeltdownAnnounced &&
            elapsed >= NonNegative(rule.ScuttleFinalMeltdownDelay))
        {
            rule.ScuttleFinalMeltdownAnnounced = true;
            AnnounceScuttleRunawayMeltdown(rule);
        }

        if (!rule.ScuttleFinalNuclearSoundPlayed &&
            elapsed >= NonNegative(rule.ScuttleFinalNuclearSoundDelay))
        {
            rule.ScuttleFinalNuclearSoundPlayed = true;
            PlayScuttleNuclearDetonationWarning(rule);
        }

        var cinematicStartedAt = startedAt + NonNegative(rule.ScuttleFinalCinematicDelay);
        if (!rule.ScuttleFinalCinematicStarted &&
            time >= cinematicStartedAt)
        {
            rule.ScuttleFinalCinematicStarted = true;
            _evacuation.StopEvacuationProgress();
            RaiseNetworkEvent(new RMCScuttleCinematicEvent(cinematicStartedAt, NonNegative(rule.ScuttleFinalSequenceDelay)), Filter.Broadcast());
        }

        UpdateScuttleHeatAura(rule);

        if (rule.ScuttleFinalDetonateAt is { } detonateAt &&
            time >= detonateAt)
        {
            DetonateScuttle(rule);
            return true;
        }

        return false;
    }

    private void SetScuttleFinalTimes(CMDistressSignalRuleComponent rule, TimeSpan finalStartedAt)
    {
        var cinematicDelay = NonNegative(rule.ScuttleFinalCinematicDelay);
        var cinematicDuration = NonNegative(rule.ScuttleFinalSequenceDelay);
        var cinematicStartedAt = finalStartedAt + cinematicDelay;

        rule.ScuttleFinalDetonateAt = cinematicStartedAt + RMCScuttleCinematicTiming.GetExplosionOffset(cinematicDuration);
        rule.ScuttleRoundEndAt = cinematicStartedAt + cinematicDuration;
    }

    private bool UpdateScuttleRoundEnd(CMDistressSignalRuleComponent rule)
    {
        if (!rule.AutoEnd || rule.Result != null)
            return false;

        var time = Timing.CurTime;
        var roundEndAt = rule.ScuttleRoundEndAt ?? time;
        rule.ScuttleRoundEndAt = roundEndAt;

        if (time < roundEndAt)
            return true;

        rule.Result = DistressSignalRuleResult.SelfDestruct;
        rule.CustomRoundEndMessage = null;
        _roundEnd.EndRound();
        return true;
    }

    private void AnnounceScuttleDeckCreak(CMDistressSignalRuleComponent rule)
    {
        var message = Loc.GetString("rmc-distress-signal-scuttle-deck-creak");
        var sound = rule.ScuttleCreakSounds.Count > 0 ? _random.Pick(rule.ScuttleCreakSounds) : null;

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(sound, filter, true);
        }

        var actors = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (actors.MoveNext(out var uid, out _, out var xform))
        {
            if (!IsAlmayerMap(xform.MapUid))
                continue;

            _popup.PopupEntity(message, uid, uid, PopupType.MediumCaution);
        }
    }

    private void AnnounceScuttleRunawayMeltdown(CMDistressSignalRuleComponent rule)
    {
        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-runaway-meltdown"),
            rule.ScuttleNoticeSound,
            "rmc-announcement-ares-command");

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(rule.ScuttleRumbleSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, rule.ScuttleMeltdownShakeDuration, rule.ScuttleMeltdownShakeIntensity);
        }

        SpawnScuttleFireAroundOverloadedReactors(rule, rule.ScuttleFinalFireRange, rule.ScuttleFinalFireIntensity, rule.ScuttleFinalFireDuration);
    }

    private void PlayScuttleNuclearDetonationWarning(CMDistressSignalRuleComponent rule)
    {
        if (rule.ScuttleNuclearDetonationSounds.Count > 0)
            _audio.PlayGlobal(_random.Pick(rule.ScuttleNuclearDetonationSounds), Filter.Broadcast(), true);

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(rule.ScuttleRumbleSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, rule.ScuttleNuclearShakeDuration, rule.ScuttleNuclearShakeIntensity);
        }
    }

    private void DetonateScuttle(CMDistressSignalRuleComponent rule)
    {
        if (rule.ScuttleDetonated)
            return;

        rule.ScuttleDetonated = true;
        _audio.PlayGlobal(rule.ScuttleDetonationSound, Filter.Broadcast(), true);

        foreach (var map in GetAlmayerMapIds())
        {
            _rmcNuke.NukeMap(map);
        }
    }

    private void AnnounceScuttleStage(CMDistressSignalRuleComponent rule, LocId message, int fireRange, int fireIntensity, int fireDuration)
    {
        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString(message),
            rule.ScuttleStageSound,
            "rmc-announcement-ares-command");

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(rule.ScuttleStageSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, rule.ScuttleStageShakeIntensity, rule.ScuttleStageShakeDuration);
        }

        SpawnScuttleFireAroundOverloadedReactors(rule, fireRange, fireIntensity, fireDuration);
    }

    private void UpdateScuttleHeatAura(CMDistressSignalRuleComponent rule)
    {
        if (!rule.ScuttleOneThirdAnnounced && !rule.ScuttleFinalSequenceStarted)
            return;

        var time = Timing.CurTime;
        rule.ScuttleNextHeatPulseAt ??= time;

        if (time < rule.ScuttleNextHeatPulseAt)
            return;

        var pulseEvery = NonNegative(rule.ScuttleHeatPulseEvery);
        if (pulseEvery == TimeSpan.Zero)
            pulseEvery = TimeSpan.FromSeconds(1);

        rule.ScuttleNextHeatPulseAt = time + pulseEvery;

        var superheated = rule.ScuttleTwoThirdsAnnounced || rule.ScuttleFinalSequenceStarted;
        ApplyScuttleHeatAura(
            superheated ? rule.ScuttleSuperheatRadius : rule.ScuttleHeatRadius,
            superheated ? rule.ScuttleSuperheatJoules : rule.ScuttleHeatJoules,
            superheated ? rule.ScuttleSuperheatDamage : rule.ScuttleHeatDamage,
            superheated
                ? "rmc-distress-signal-scuttle-superheat-aura"
                : "rmc-distress-signal-scuttle-heat-aura");
    }

    private void ApplyScuttleHeatAura(float radius, float heatJoules, DamageSpecifier fallbackDamage, LocId message)
    {
        var damaged = new HashSet<EntityUid>();
        var targets = new HashSet<Entity<DamageableComponent>>();

        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out var reactorUid, out var reactor, out var xform))
        {
            if (!reactor.Overloaded || !IsAlmayerMap(xform.MapUid))
                continue;

            targets.Clear();
            _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(reactorUid, xform), radius, targets);
            foreach (var (target, _) in targets)
            {
                if (target == reactorUid || !HasComp<MobStateComponent>(target))
                    continue;

                if (!damaged.Add(target))
                    continue;

                if (TryComp<TemperatureComponent>(target, out var temperature))
                {
                    _temperature.ChangeHeat(target, heatJoules, temperature: temperature);

                    // RMC mobs currently carry body temperature state, but their base Temperature
                    // component does not define HeatDamage, so keep scuttle lethal without broad
                    // changes to every temperature source in RMC.
                    if (temperature.HeatDamage.Empty)
                        _damageable.TryChangeDamage(target, fallbackDamage, true, false, origin: reactorUid);
                }
                else
                {
                    _damageable.TryChangeDamage(target, fallbackDamage, true, false, origin: reactorUid);
                }

                _popup.PopupEntity(Loc.GetString(message), target, target, PopupType.SmallCaution);
            }
        }
    }

    private int CountOverloadedScuttleReactors()
    {
        var count = 0;
        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out _, out var reactor, out var xform))
        {
            if (reactor.Overloaded && IsAlmayerMap(xform.MapUid))
                count++;
        }

        return count;
    }

    private int CountMappedScuttleReactors()
    {
        var count = 0;
        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out _, out _, out var xform))
        {
            if (IsAlmayerMap(xform.MapUid))
                count++;
        }

        return count;
    }

    private int GetScuttleTotalReactors(CMDistressSignalRuleComponent rule, int overloadedReactors)
    {
        if (rule.ScuttleTotalReactors <= 0)
            rule.ScuttleTotalReactors = CountMappedScuttleReactors();

        return Math.Max(rule.ScuttleTotalReactors, overloadedReactors);
    }

    private TimeSpan GetScuttleRequiredDuration(CMDistressSignalRuleComponent rule, int overloadedReactors, int totalReactors)
    {
        if (totalReactors <= 0)
            return rule.ScuttleMaxDuration;

        var fraction = Math.Clamp(overloadedReactors / (double) totalReactors, 0, 1);
        var seconds = rule.ScuttleMaxDuration.TotalSeconds +
                      (rule.ScuttleMinDuration.TotalSeconds - rule.ScuttleMaxDuration.TotalSeconds) * fraction;
        return TimeSpan.FromSeconds(seconds);
    }

    private bool HasReachedScuttleProgress(CMDistressSignalRuleComponent rule, TimeSpan required, double fraction)
    {
        return required > TimeSpan.Zero && rule.ScuttleProgress.TotalSeconds / required.TotalSeconds >= fraction;
    }

    private static TimeSpan NonNegative(TimeSpan time)
    {
        return time < TimeSpan.Zero ? TimeSpan.Zero : time;
    }

    private void SpawnScuttleFireAroundOverloadedReactors(CMDistressSignalRuleComponent rule, int range, int intensity, int duration)
    {
        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out var uid, out var reactor, out var xform))
        {
            if (!reactor.Overloaded || !IsAlmayerMap(xform.MapUid))
                continue;

            _rmcFlammable.SpawnFireDiamond(rule.ScuttleFire, _transform.GetMoverCoordinates(uid), range, intensity, duration);
        }
    }

    private HashSet<MapId> GetAlmayerMapIds()
    {
        var maps = new HashSet<MapId>();
        var almayers = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
        while (almayers.MoveNext(out _, out _, out var xform))
        {
            maps.Add(xform.MapID);
        }

        return maps;
    }

    private bool IsAlmayerMap(EntityUid? mapUid)
    {
        return mapUid != null && HasComp<AlmayerComponent>(mapUid.Value);
    }

    private void OnFusionReactorOverloadStatus(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorOverloadStatusEvent args)
    {
        var rule = TryGetActiveRule();
        if (rule == null ||
            !IsAlmayerMap(Transform(ent).MapUid) ||
            (!rule.ScuttleUnlocked && !ent.Comp.Overloaded))
        {
            return;
        }

        var overloadedReactors = CountOverloadedScuttleReactors();
        var totalReactors = GetScuttleTotalReactors(rule, overloadedReactors);
        var eta = Loc.GetString("rmc-fusion-reactor-overload-eta-never");
        var progress = 0;

        if (rule.ScuttleFinalSequenceStarted)
        {
            eta = FormatScuttleEta((rule.ScuttleFinalDetonateAt ?? Timing.CurTime) - Timing.CurTime);
            progress = 100;
        }
        else if (overloadedReactors > 0)
        {
            var required = GetScuttleRequiredDuration(rule, overloadedReactors, totalReactors);
            var remaining = required - rule.ScuttleProgress;
            eta = FormatScuttleEta(remaining);
            progress = (int) Math.Clamp(rule.ScuttleProgress.TotalSeconds / required.TotalSeconds * 100, 0, 100);
        }

        args.Text = Loc.GetString(ent.Comp.Overloaded
                ? "rmc-fusion-reactor-overload-examine-active"
                : "rmc-fusion-reactor-overload-examine-available",
            ("reactors", overloadedReactors),
            ("totalReactors", totalReactors),
            ("eta", eta),
            ("progress", progress));
    }

    private string FormatScuttleEta(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
            return Loc.GetString("rmc-fusion-reactor-overload-eta-imminent");

        var totalSeconds = (int) Math.Ceiling(remaining.TotalSeconds);
        return Loc.GetString(
            "rmc-fusion-reactor-overload-eta-time",
            ("minutes", totalSeconds / 60),
            ("seconds", totalSeconds % 60));
    }

    private void OnFusionReactorCanOverload(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorCanOverloadEvent args)
    {
        var rule = TryGetActiveRule();
        if (rule == null ||
            !rule.ScuttleUnlocked ||
            rule.ScuttleDetonated ||
            rule.ScuttleFinalSequenceStarted)
        {
            return;
        }

        if (!IsAlmayerMap(Transform(ent).MapUid))
            return;

        args.CanOverload = true;
    }

    private void OnFusionReactorOverloadChanged(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorOverloadChangedEvent args)
    {
        if (!args.Overloaded || !IsAlmayerMap(Transform(ent).MapUid))
            return;

        var rule = TryGetActiveRule();
        if (rule == null || !rule.ScuttleUnlocked || rule.ScuttleFirstOverloadAnnounced)
            return;

        rule.ScuttleFirstOverloadAnnounced = true;

        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-first-overload"),
            announcement: "rmc-announcement-ares-command");

        _xenoAnnounce.AnnounceQueenMother(Loc.GetString("rmc-xeno-announcement-scuttle-first-overload"));
    }
}
