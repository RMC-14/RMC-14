using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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

    private static readonly EntProtoId ScuttleFire = "RMCTileFire";
    private static readonly SoundSpecifier ScuttleStageSound = new SoundPathSpecifier("/Audio/Machines/warning_buzzer.ogg");
    private static readonly SoundSpecifier ScuttleFinalSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Nuke/nuke.ogg");
    private static readonly SoundSpecifier ScuttleDetonationSound = new SoundPathSpecifier("/Audio/Effects/explosionfar.ogg");

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
        {
            if (rule.ScuttleFinalDetonateAt is { } detonateAt &&
                time >= detonateAt)
            {
                DetonateScuttle(rule);
                return true;
            }

            return false;
        }

        var overloaded = CountOverloadedScuttleReactors();
        if (overloaded <= 0)
            return false;

        rule.ScuttleProgress += TimeSpan.FromSeconds(frameTime);

        var required = GetScuttleRequiredDuration(rule, overloaded);
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

        if (rule.ScuttleProgress >= required)
            StartScuttleFinalSequence(rule);

        return false;
    }

    private void UnlockScuttle(CMDistressSignalRuleComponent rule)
    {
        rule.ScuttleUnlocked = true;
        rule.ScuttleUnlockAt = null;

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
        rule.ScuttleFinalDetonateAt = Timing.CurTime + rule.ScuttleFinalSequenceDelay;

        _marineAnnounce.AnnounceARESStaging(
            default,
            Loc.GetString("rmc-distress-signal-scuttle-final"),
            ScuttleFinalSound,
            "rmc-announcement-ares-command");

        foreach (var map in GetAlmayerMapIds())
        {
            var filter = Filter.BroadcastMap(map);
            _audio.PlayGlobal(ScuttleFinalSound, filter, true);
            _rmcCameraShake.ShakeCamera(filter, ScuttleFinalShakeIntensity, ScuttleFinalShakeDuration);
        }

        SpawnScuttleFireAroundOverloadedReactors(ScuttleFinalFireRange, ScuttleFinalFireIntensity, ScuttleFinalFireDuration);
    }

    private void DetonateScuttle(CMDistressSignalRuleComponent rule)
    {
        if (rule.ScuttleDetonated)
            return;

        rule.ScuttleDetonated = true;

        foreach (var map in GetAlmayerMapIds())
        {
            _audio.PlayGlobal(ScuttleDetonationSound, Filter.BroadcastMap(map), true);
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

    private TimeSpan GetScuttleRequiredDuration(CMDistressSignalRuleComponent rule, int overloadedReactors)
    {
        if (rule.ScuttleMaxReactors <= 0)
            return rule.ScuttleMaxDuration;

        var fraction = Math.Clamp(overloadedReactors / (double) rule.ScuttleMaxReactors, 0, 1);
        var seconds = rule.ScuttleMaxDuration.TotalSeconds +
                      (rule.ScuttleMinDuration.TotalSeconds - rule.ScuttleMaxDuration.TotalSeconds) * fraction;
        return TimeSpan.FromSeconds(seconds);
    }

    private bool HasReachedScuttleProgress(CMDistressSignalRuleComponent rule, TimeSpan required, double fraction)
    {
        return required > TimeSpan.Zero && rule.ScuttleProgress.TotalSeconds / required.TotalSeconds >= fraction;
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
        var eta = Loc.GetString("rmc-fusion-reactor-overload-eta-never");
        var progress = 0;

        if (rule.ScuttleFinalSequenceStarted)
        {
            eta = FormatScuttleEta((rule.ScuttleFinalDetonateAt ?? Timing.CurTime) - Timing.CurTime);
            progress = 100;
        }
        else if (overloadedReactors > 0)
        {
            var required = GetScuttleRequiredDuration(rule, overloadedReactors);
            var remaining = required - rule.ScuttleProgress;
            eta = FormatScuttleEta(remaining);
            progress = (int) Math.Clamp(rule.ScuttleProgress.TotalSeconds / required.TotalSeconds * 100, 0, 100);
        }

        args.Text = Loc.GetString(ent.Comp.Overloaded
                ? "rmc-fusion-reactor-overload-examine-active"
                : "rmc-fusion-reactor-overload-examine-available",
            ("reactors", overloadedReactors),
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
