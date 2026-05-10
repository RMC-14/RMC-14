using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Rules;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Server.Temperature.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const int ScuttleStageFireRange = 1;
    private const int ScuttleStageFireIntensity = 8;
    private const int ScuttleStageFireDuration = 18;
    private const int ScuttleFinalFireRange = 2;
    private const int ScuttleFinalFireIntensity = 12;
    private const int ScuttleFinalFireDuration = 35;
    private const int ScuttleStageShakeIntensity = 4;
    private const int ScuttleFinalShakeIntensity = 12;
    private const int ScuttleStageShakeDuration = 2;
    private const int ScuttleFinalShakeDuration = 5;
    private const int ScuttleMeltdownShakeIntensity = 4;
    private const int ScuttleMeltdownShakeDuration = 20;
    private const int ScuttleNuclearShakeIntensity = 4;
    private const int ScuttleNuclearShakeDuration = 110;
    private const float ScuttleHeatRadius = 3.5f;
    private const float ScuttleSuperheatRadius = 5f;
    private const float ScuttleHeatJoules = 45000f;
    private const float ScuttleSuperheatJoules = 75000f;

    private static readonly EntProtoId ScuttleFire = "RMCTileFire";
    private static readonly DamageSpecifier ScuttleHeatDamage = new() { DamageDict = { { "Heat", 5 } } };
    private static readonly DamageSpecifier ScuttleSuperheatDamage = new() { DamageDict = { { "Heat", 10 } } };
    private static readonly SoundSpecifier ScuttleStageSound = new SoundPathSpecifier("/Audio/Machines/warning_buzzer.ogg");
    private static readonly SoundSpecifier ScuttleDetonationSound = new SoundPathSpecifier("/Audio/Effects/explosionfar.ogg");
    private static readonly SoundSpecifier ScuttleNoticeSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");
    private static readonly SoundSpecifier ScuttleRumbleSound = new SoundPathSpecifier(
        "/Audio/Magic/rumble.ogg",
        AudioParams.Default.WithVolume(3f));
    private static readonly SoundSpecifier[] ScuttleCreakSounds =
    [
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak1.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak2.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak3.ogg"),
    ];
    private static readonly SoundSpecifier[] ScuttleNuclearDetonationSounds =
    [
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/nuclear_detonation1.ogg", AudioParams.Default.WithVolume(4f)),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/nuclear_detonation2.ogg", AudioParams.Default.WithVolume(4f)),
    ];

    private bool UpdateScuttle(CMDistressSignalRuleComponent rule, float frameTime)
    {
        if (rule.Result != null || rule.ScuttleDetonated)
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
            AnnounceScuttleStage("rmc-distress-signal-scuttle-stage-one", ScuttleStageFireRange, ScuttleStageFireIntensity, ScuttleStageFireDuration);
        }

        if (!rule.ScuttleHalfwayAnnounced && HasReachedScuttleProgress(rule, required, 0.5))
        {
            rule.ScuttleHalfwayAnnounced = true;
            AnnounceScuttleStage("rmc-distress-signal-scuttle-halfway", ScuttleStageFireRange, ScuttleStageFireIntensity, ScuttleStageFireDuration);
        }

        if (!rule.ScuttleTwoThirdsAnnounced && HasReachedScuttleProgress(rule, required, 2.0 / 3.0))
        {
            rule.ScuttleTwoThirdsAnnounced = true;
            AnnounceScuttleStage("rmc-distress-signal-scuttle-stage-two", ScuttleFinalFireRange, ScuttleFinalFireIntensity, ScuttleFinalFireDuration);
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

        rule.ScuttleFinalSequenceStarted = true;
        rule.ScuttleFinalStartedAt = Timing.CurTime;
        rule.ScuttleFinalDetonateAt = Timing.CurTime +
                                      NonNegative(rule.ScuttleFinalCinematicDelay) +
                                      NonNegative(rule.ScuttleFinalSequenceDelay);

        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-final"),
            ScuttleStageSound,
            "rmc-announcement-ares-command");

        AnnounceScuttleDeckCreak();
        SpawnScuttleFireAroundOverloadedReactors(ScuttleFinalFireRange, ScuttleFinalFireIntensity, ScuttleFinalFireDuration);
    }

    private bool UpdateScuttleFinalSequence(CMDistressSignalRuleComponent rule)
    {
        var time = Timing.CurTime;
        var startedAt = rule.ScuttleFinalStartedAt ?? time;
        if (rule.ScuttleFinalStartedAt == null)
            rule.ScuttleFinalStartedAt = startedAt;

        var elapsed = time - startedAt;
        if (!rule.ScuttleFinalMeltdownAnnounced &&
            elapsed >= NonNegative(rule.ScuttleFinalMeltdownDelay))
        {
            rule.ScuttleFinalMeltdownAnnounced = true;
            AnnounceScuttleRunawayMeltdown();
        }

        if (!rule.ScuttleFinalNuclearSoundPlayed &&
            elapsed >= NonNegative(rule.ScuttleFinalNuclearSoundDelay))
        {
            rule.ScuttleFinalNuclearSoundPlayed = true;
            PlayScuttleNuclearDetonationWarning();
        }

        if (!rule.ScuttleFinalCinematicStarted &&
            elapsed >= NonNegative(rule.ScuttleFinalCinematicDelay))
        {
            rule.ScuttleFinalCinematicStarted = true;
            RaiseNetworkEvent(new RMCScuttleCinematicEvent(NonNegative(rule.ScuttleFinalSequenceDelay)), Filter.Broadcast());
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

    private void AnnounceScuttleDeckCreak()
    {
        var message = Loc.GetString("rmc-distress-signal-scuttle-deck-creak");
        var sound = _random.Pick(ScuttleCreakSounds);

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

    private void AnnounceScuttleRunawayMeltdown()
    {
        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-runaway-meltdown"),
            ScuttleNoticeSound,
            "rmc-announcement-ares-command");

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(ScuttleRumbleSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, ScuttleMeltdownShakeDuration, ScuttleMeltdownShakeIntensity);
        }

        SpawnScuttleFireAroundOverloadedReactors(ScuttleFinalFireRange, ScuttleFinalFireIntensity, ScuttleFinalFireDuration);
    }

    private void PlayScuttleNuclearDetonationWarning()
    {
        _audio.PlayGlobal(_random.Pick(ScuttleNuclearDetonationSounds), Filter.Broadcast(), true);

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(ScuttleRumbleSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, ScuttleNuclearShakeDuration, ScuttleNuclearShakeIntensity);
        }
    }

    private void DetonateScuttle(CMDistressSignalRuleComponent rule)
    {
        if (rule.ScuttleDetonated)
            return;

        rule.ScuttleDetonated = true;
        _audio.PlayGlobal(ScuttleDetonationSound, Filter.Broadcast(), true);

        foreach (var map in GetAlmayerMapIds())
        {
            _rmcNuke.NukeMap(map);
        }

        if (!rule.AutoEnd || rule.Result != null)
            return;

        rule.Result = DistressSignalRuleResult.SelfDestruct;
        rule.CustomRoundEndMessage = null;
        _roundEnd.EndRound();
    }

    private void AnnounceScuttleStage(LocId message, int fireRange, int fireIntensity, int fireDuration)
    {
        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString(message),
            ScuttleStageSound,
            "rmc-announcement-ares-command");

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(ScuttleStageSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, ScuttleStageShakeIntensity, ScuttleStageShakeDuration);
        }

        SpawnScuttleFireAroundOverloadedReactors(fireRange, fireIntensity, fireDuration);
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
            superheated ? ScuttleSuperheatRadius : ScuttleHeatRadius,
            superheated ? ScuttleSuperheatJoules : ScuttleHeatJoules,
            superheated ? ScuttleSuperheatDamage : ScuttleHeatDamage,
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

    private void SpawnScuttleFireAroundOverloadedReactors(int range, int intensity, int duration)
    {
        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out var uid, out var reactor, out var xform))
        {
            if (!reactor.Overloaded || !IsAlmayerMap(xform.MapUid))
                continue;

            _rmcFlammable.SpawnFireDiamond(ScuttleFire, _transform.GetMoverCoordinates(uid), range, intensity, duration);
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
            args.Failure = "rmc-fusion-reactor-overload-unavailable";
            return;
        }

        if (!IsAlmayerMap(Transform(ent).MapUid))
        {
            args.Failure = "rmc-fusion-reactor-overload-not-almayer";
            return;
        }

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
