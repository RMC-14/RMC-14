using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._RMC14.Armor.Ghillie;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const int LastMarineEquipmentCooldownHours = 2;

    /// <summary>
    /// Performs all round end condition checks including hijack status, faction live counts,
    /// queen death timer, and last marine equipment effects.
    /// </summary>
    private void CheckRoundShouldEnd()
    {
        var distress = TryGetActiveRule();
        if (distress == null)
            return;

        distress.NextCheck ??= Timing.CurTime + distress.CheckEvery;

        if (distress.ForceEndAt != null && Timing.CurTime >= distress.ForceEndAt)
        {
            EndRound(distress, DistressSignalRuleResult.MinorXenoVictory, "rmc-distress-signal-minorxenovictory-timeout");
            return;
        }

        UpdateHijackState(distress);
        RefreshAlmayerMaps();

        var time = Timing.CurTime;
        var xenosAlive = CheckAliveXenos(distress, time);
        var marinesAlive = CheckAliveMarines(distress, time);

        if (TryResolveRoundEnd(distress, xenosAlive, marinesAlive))
            return;

        ApplyLastMarineEffects(time);
        CheckQueenDeath(distress);
    }

    private void UpdateHijackState(CMDistressSignalRuleComponent distress)
    {
        var hijack = false;
        var dropshipQuery = EntityQueryEnumerator<DropshipComponent>();
        while (dropshipQuery.MoveNext(out var dropship))
        {
            if (dropship.Crashed)
                hijack = true;
        }

        var time = Timing.CurTime;
        if (!distress.Hijack && hijack)
        {
            distress.Hijack = true;
            distress.AbandonedAt ??= time + distress.AbandonedDelay;
        }
    }

    private void RefreshAlmayerMaps()
    {
        _almayerMaps.Clear();
        var almayerQuery = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
        while (almayerQuery.MoveNext(out _, out var xform))
        {
            _almayerMaps.Add(xform.MapID);
        }
    }

    private bool CheckAliveXenos(CMDistressSignalRuleComponent distress, TimeSpan time)
    {
        var xenos = EntityQueryEnumerator<ActorComponent, XenoComponent, MobStateComponent, TransformComponent>();
        while (xenos.MoveNext(out var xenoId, out _, out var xeno, out var mobState, out var xform))
        {
            if (!xeno.ContributesToVictory)
                continue;

            if (HasComp<ThunderdomeMapComponent>(xform.MapUid) ||
                (_containers.IsEntityInContainer(xenoId) && !HasComp<XenoEvolutionGranterComponent>(xenoId)))
                continue;

            if (_mobState.IsAlive(xenoId, mobState) &&
                (distress.AbandonedAt == null ||
                 time < distress.AbandonedAt ||
                 !distress.Hijack ||
                 _almayerMaps.Contains(xform.MapID)))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckAliveMarines(CMDistressSignalRuleComponent distress, TimeSpan time)
    {
        var marines = EntityQueryEnumerator<ActorComponent, MarineComponent, MobStateComponent, TransformComponent>();
        _marineList.Clear();
        while (marines.MoveNext(out var marineId, out _, out _, out var mobState, out var xform))
        {
            if (HasComp<VictimInfectedComponent>(marineId) ||
                HasComp<VictimBurstComponent>(marineId) ||
                _xenoNestedQuery.HasComp(marineId))
            {
                continue;
            }

            if (_containers.IsEntityInContainer(marineId))
                continue;

            if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                continue;

            if (_mobState.IsAlive(marineId, mobState) &&
                (distress.AbandonedAt == null ||
                 time < distress.AbandonedAt ||
                 !distress.Hijack ||
                 _almayerMaps.Contains(xform.MapID)))
            {
                _marineList.Add(marineId);
            }

            if (_marineList.Count >= 2)
                break;
        }
        return _marineList.Count > 0;
    }

    /// <summary>
    /// Determines the round result based on alive xenos and marines, and triggers the round end if resolved.
    /// </summary>
    /// <returns>True if the round end was triggered, false otherwise.</returns>
    private bool TryResolveRoundEnd(CMDistressSignalRuleComponent distress, bool xenosAlive, bool marinesAlive)
    {
        if (xenosAlive && !marinesAlive)
        {
            EndRound(distress, DistressSignalRuleResult.MajorXenoVictory);
            return true;
        }

        if (!xenosAlive && marinesAlive)
        {
            EndRound(distress,
                distress.Hijack
                    ? DistressSignalRuleResult.MinorXenoVictory
                    : DistressSignalRuleResult.MajorMarineVictory);
            return true;
        }

        if (!xenosAlive && !marinesAlive)
        {
            EndRound(distress, DistressSignalRuleResult.AllDied);
            return true;
        }

        return false;
    }

    private void ApplyLastMarineEffects(TimeSpan time)
    {
        if (_marineList.Count != 1)
            return;

        var lastMarine = _marineList.Last();
        var cooldownEnd = time + TimeSpan.FromHours(LastMarineEquipmentCooldownHours);

        var cloaks = EntityQueryEnumerator<ThermalCloakComponent>();
        while (cloaks.MoveNext(out var cloakId, out var cloak))
        {
            if (!cloak.Enabled)
                continue;

            _thermalCloak.SetInvisibility((cloakId, cloak), lastMarine, false, true);
            _actions.SetCooldown(cloak.Action, time, cooldownEnd);
            _actions.SetUseDelay(cloak.Action, TimeSpan.FromHours(LastMarineEquipmentCooldownHours));
        }

        var ghillies = EntityQueryEnumerator<GhillieSuitComponent>();
        while (ghillies.MoveNext(out var ghillieId, out var ghillie))
        {
            if (!ghillie.Enabled)
                continue;

            _ghillieSuit.ToggleInvisibility((ghillieId, ghillie), lastMarine, false);
            _actions.SetCooldown(ghillie.Action, time, cooldownEnd);
            _actions.SetUseDelay(ghillie.Action, TimeSpan.FromHours(LastMarineEquipmentCooldownHours));
        }
    }

    private void CheckQueenDeath(CMDistressSignalRuleComponent distress)
    {
        if (_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
        {
            distress.QueenDiedCheck = null;
            return;
        }

        distress.QueenDiedCheck ??= Timing.CurTime + distress.QueenDiedDelay;
        if (distress.QueenDiedCheck == null)
            return;

        if (Timing.CurTime >= distress.QueenDiedCheck)
        {
            EndRoundForQueenDeath(distress);
        }
    }

    private void OnRoundEndMessage(RoundEndMessageEvent ev)
    {
        var distress = TryGetActiveRule();
        if (distress == null)
            return;

        if (distress.Result == DistressSignalRuleResult.None)
            return;

        var audio = distress.Result switch
        {
            DistressSignalRuleResult.MajorMarineVictory => distress.MajorMarineAudio,
            DistressSignalRuleResult.MinorMarineVictory => distress.MinorMarineAudio,
            DistressSignalRuleResult.MajorXenoVictory => distress.MajorXenoAudio,
            DistressSignalRuleResult.MinorXenoVictory => distress.MinorXenoAudio,
            _ => null,
        };

        if (audio != null)
            _audio.PlayGlobal(audio, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-8));
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        InvalidateActiveRule();
        StartPlanetVote();
        ResetSelectedPlanet();
        _config.SetCVar(CCVars.GameDisallowLateJoins, false);

        if (!_autoBalance)
            return;

        var rules = QueryAllRules();
        while (rules.MoveNext(out var comp, out _))
        {
            var adjust = comp.Result switch
            {
                DistressSignalRuleResult.None => 0,
                DistressSignalRuleResult.MajorMarineVictory => -1,
                DistressSignalRuleResult.MinorMarineVictory => -1,
                DistressSignalRuleResult.MajorXenoVictory => 1,
                DistressSignalRuleResult.MinorXenoVictory => 0,
                DistressSignalRuleResult.AllDied => 0,
                null => 0,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (adjust == 0)
                continue;

            var value = _marinesPerXeno;
            value += adjust * _autoBalanceStep;

            if (value > _autoBalanceMax)
                value = _autoBalanceMax;
            else if (value < _autoBalanceMin)
                value = _autoBalanceMin;

            _config.SetCVar(RMCCVars.CMMarinesPerXeno, value);
            break;
        }
    }

    /// <summary>
    /// Ends the round with the specified result, handling victory conditions and announcements.
    /// </summary>
    private void EndRound(CMDistressSignalRuleComponent rule, DistressSignalRuleResult result, LocId? customMessage = null)
    {
        if (!rule.AutoEnd)
            return;

        if (rule.StartTime == null || Timing.CurTime - rule.StartTime < rule.RoundEndCheckDelay)
            return;

        Log.Info($"Attempting to set {nameof(rule)} result to {result}");
        if (rule.Result != null)
            return;

        rule.Result = result;
        rule.CustomRoundEndMessage = customMessage;

        if (result == DistressSignalRuleResult.MajorMarineVictory)
        {
            if (rule.XenoMap is { } xenoMap)
            {
                var rmcAmbientComp = EnsureComp<RMCAmbientLightComponent>(xenoMap);
                var rmcAmbientEffectComp = EnsureComp<RMCAmbientLightEffectsComponent>(xenoMap);
                var colorSequence = _rmcAmbientLight.ProcessPrototype(rmcAmbientEffectComp.Sunrise);
                _rmcAmbientLight.SetColor((xenoMap, rmcAmbientComp), colorSequence, _sunriseDuration);
            }

            var ares = _ares.EnsureARES();
            _marineAnnounce.AnnounceRadio(ares,
                Loc.GetString("rmc-distress-signal-bioscan-complete"),
                rule.AllClearChannel);
            _marineAnnounce.AnnounceRadio(ares,
                Loc.GetString("rmc-distress-signal-saving-report"),
                rule.AllClearChannel);
            _marineAnnounce.AnnounceRadio(ares,
                Loc.GetString("rmc-distress-signal-final-scan"),
                rule.AllClearChannel);
            rule.EndAtAllClear ??= Timing.CurTime + rule.AllClearEndDelay;
        }
        else
        {
            _roundEnd.EndRound();
        }
    }

    private void OnMobStateChanged<T>(Entity<T> ent, ref MobStateChangedEvent args) where T : IComponent?
    {
        if (args.NewMobState != MobState.Dead) return;
        RemCompDeferred<GhostRoleComponent>(ent);
        CheckRoundShouldEnd();
    }

    private void OnCompRemove<T>(Entity<T> ent, ref ComponentRemove args) where T : IComponent?
    {
        CheckRoundShouldEnd();
    }

    private void OnMapInit(Entity<XenoEvolutionGranterComponent> ent, ref MapInitEvent args)
    {
        CheckRoundShouldEnd();
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CMDistressSignalRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var result = component.Result ??= DistressSignalRuleResult.None;
        args.AddLine(component.CustomRoundEndMessage != null
            ? $"{Loc.GetString(component.CustomRoundEndMessage)}"
            : $"{Loc.GetString($"cm-distress-signal-{result.ToString().ToLower()}")}");

        args.AddLine(string.Empty);

        if (_gameRulesExtras.MemorialEntry(ref args))
            args.AddLine(string.Empty);

        if (_gameRulesExtras.MarineAwards(ref args))
            args.AddLine(string.Empty);

        _gameRulesExtras.XenoAwards(ref args);
    }

    protected override void OnStartAttempt(Entity<CMDistressSignalRuleComponent, GameRuleComponent> gameRule, RoundStartAttemptEvent ev)
    {
        if (ev.Forced || ev.Cancelled)
            return;

        if (!gameRule.Comp1.RequireXenoPlayers)
            return;

        var query = QueryAllRules();
        while (query.MoveNext(out _, out var distress, out _))
        {
            var xenoCandidates = 0;
            foreach (var player in ev.Players)
            {
                if (!_prefsManager.TryGetCachedPreferences(player.UserId, out var preferences)) continue;
                if (preferences.GetProfile(preferences.SelectedCharacterIndex) is not HumanoidCharacterProfile profile)
                    continue;

                if (profile.JobPriorities.TryGetValue(distress.XenoSelectableJob, out var xenoPriority) &&
                    xenoPriority > JobPriority.Never || profile.JobPriorities.TryGetValue(distress.QueenJob, out var queenPriority) &&
                    queenPriority > JobPriority.Never)
                {
                    xenoCandidates++;
                }
            }

            if (xenoCandidates >= _xenosMinimum)
                continue;

            var msg = Loc.GetString("rmc-distress-signal-admin-start-fail",
                ("minimum", _xenosMinimum),
                ("candidates", xenoCandidates));
            _chatManager.SendAdminAnnouncement(msg);
            _chatManager.DispatchServerAnnouncement(msg);
            ev.Cancel();
        }
    }
}
