using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Stats;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Stats;

public sealed class StatTrackingSystem : SharedStatTrackingSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundEndStatsAppendEvent>(OnRoundEndStatTextAppend);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        PlayerStats.Clear();

        TotalMarines = 0;
        TotalMarineDeaths = 0;
        TotalMarinePermaDeaths = 0;
        TotalDamageReceived= 0;
        TotalMarineProjectiles = 0;
        TotalMarineProjectileHits = 0;
        TotalFriendlyFireIncidents = 0;
        TotalFriendlyFireKills = 0;
        TotalLarvaExtractions = 0;
        TotalUsedRequisitionsBudget = 0;
        TotalSupplyDrops = 0;

        TotalXenos = 0;
        TotalXenoDeaths = 0;
        TotalXenoDamageReceived= 0;
        TotalLesserXenos = 0;
        TotalPlayerParasites = 0;
        TotalXenoProjectiles = 0;
        TotalXenoProjectileHits = 0;
        TotalXenoMeleeHits = 0;
        TotalInfected = 0;
        TotalBursts = 0;
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        RoundStartTime = Timing.CurTime;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (HasComp<RMCAdminSpawnedComponent>(ev.Mob))
            return;

        if (HasComp<XenoParasiteComponent>(ev.Mob))
        {
            if (TryComp(ev.Mob, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.ParasiteSpawn);

            TotalPlayerParasites++;
            return;
        }

        if (TryComp(ev.Mob, out XenoComponent? xeno))
        {
            if (xeno.Role == LesserJob)
            {
                if (TryComp(ev.Mob, out ActorComponent? actor))
                    ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.LesserDroneSpawn);

                TotalLesserXenos++;
                return;
            }

            TotalXenos++;
        }
        else if (HasComp<MarineComponent>(ev.Mob))
            TotalMarines++;
    }

    private void OnRoundEndStatTextAppend(RoundEndStatsAppendEvent endEvent)
    {
        // Marine stats
        endEvent.AddLine(Loc.GetString("rmc-distress-signal-round-stat-marine-header"));

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-total-marines", TotalMarines);

        var (mostDeathsName, mostDeaths) = GetHighScore(player => player.Marine.TotalMarineDeaths);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-marine-deaths", TotalMarineDeaths, mostDeathsName, mostDeaths);

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-marine-perma-deaths", TotalMarinePermaDeaths);

        var (mostDamageReceivedName, mostDamageReceived) = GetHighScore(player => (int) player.Marine.TotalDamageReceived);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-damage-received", (int) TotalDamageReceived, mostDamageReceivedName, mostDamageReceived);

        var (mostDamageHealedName, moxeDamageHealed) = GetHighScore(player => (int) player.Marine.TotalDamageHealed);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-damage-healed", (int) TotalDamageHealed, mostDamageHealedName, moxeDamageHealed);

        var (mostProjectilesName, mostProjectiles) = GetHighScore(player => player.Marine.TotalProjectiles);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-marine-projectiles-fired", TotalMarineProjectiles, mostProjectilesName, mostProjectiles);

        var (mostProjectileHitsName, mostProjectileHits) = GetHighScore(player => player.Marine.TotalProjectileHits);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-marine-projectile-hits", TotalMarineProjectileHits, mostProjectileHitsName, mostProjectileHits);

        var (mostFriendlyFireIncidentsName, mostFriendlyFireIncidents) = GetHighScore(player => player.Marine.TotalFriendlyFireIncidents);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-friendly-fire-incidents", TotalFriendlyFireIncidents, mostFriendlyFireIncidentsName, mostFriendlyFireIncidents);

        var (mostFriendlyFireKillsName, mostFriendlyFireKills) = GetHighScore(player => player.Marine.TotalFriendlyFireKills);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-friendly-fire-kills", TotalFriendlyFireKills, mostFriendlyFireKillsName, mostFriendlyFireKills);

        var (mostLarvaExtractionsName, mostLarvaExtractions) = GetHighScore(player => player.Marine.TotalLarvaExtractions);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-larva-extractions", TotalLarvaExtractions, mostLarvaExtractionsName, mostLarvaExtractions);

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-used-requisitions-budget", TotalUsedRequisitionsBudget);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-supply-drops", TotalSupplyDrops);

        FixedPoint2 currentPoints = 0;
        FixedPoint2 totalPoints = 0;
        var query = EntityQueryEnumerator<ViewIntelObjectivesComponent>();
        while (query.MoveNext(out _, out var viewIntel))
        {
            currentPoints = viewIntel.Tree.Points;
            totalPoints = viewIntel.Tree.TotalEarned;
            break;
        }

        endEvent.AddLine(Loc.GetString(
            "rmc-distress-signal-round-stat-intel-points",
            ("earned", totalPoints),
            ("spent", totalPoints - currentPoints)));

        endEvent.AddLine(string.Empty);

        // Xeno stats
        endEvent.AddLine(Loc.GetString("rmc-distress-signal-round-stat-xeno-header"));

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-total-xenos", TotalXenos);

        var (mostXenoDeathsName, mostXenoDeaths) = GetHighScore(player => player.Xeno.TotalDeaths);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-deaths", TotalXenoDeaths, mostXenoDeathsName, mostXenoDeaths);

        var (mostXenoDamageReceivedName, mostXenoDamageReceived) = GetHighScore(player => (int) player.Xeno.TotalDamageReceived);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-damage-received", (int) TotalXenoDamageReceived, mostXenoDamageReceivedName, mostXenoDamageReceived);

        var (mostXenoDamageHealedName, moxeXenoDamageHealed) = GetHighScore(player => (int) player.Xeno.TotalDamageHealed);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-damage-healed", (int) TotalXenoDamageHealed, mostXenoDamageHealedName, moxeXenoDamageHealed);

        var (mostLesserXenoSpawnsName, mostLesserXenoSpawns) = GetHighScore(player => player.Xeno.TotalLesserDroneSpawns);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-lesser-drones", TotalLesserXenos, mostLesserXenoSpawnsName, mostLesserXenoSpawns);

        var (mostParasitesName, mostParasites) = GetHighScore(player => player.Xeno.TotalParasiteSpawns);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-player-parasites", TotalPlayerParasites, mostParasitesName, mostParasites);

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-infected", TotalInfected);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-bursts", TotalBursts);

        var (mostXenoProjectilesName, mostXenoProjectiles) = GetHighScore(player => player.Xeno.TotalProjectiles);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-projectiles-fired", TotalXenoProjectiles, mostXenoProjectilesName, mostXenoProjectiles);

        var (mostXenoProjectileHitsName, mostXenoProjectileHits) = GetHighScore(player => player.Xeno.TotalProjectileHits);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-projectile-hits", TotalXenoProjectileHits, mostXenoProjectileHitsName, mostXenoProjectileHits);

        var (mostMeleeHitsName, mostMeleeHits) = GetHighScore(player => player.Xeno.TotalMeleeHits);
        AddPlayerTrackedStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-melee-hits", TotalXenoMeleeHits, mostMeleeHitsName, mostMeleeHits);

        endEvent.AddLine(string.Empty);
    }

    private void AddStat(ref RoundEndStatsAppendEvent  endEvent, string id, int value)
    {
        endEvent.AddLine(Loc.GetString(id, ("count", value)));
    }

    private void AddPlayerTrackedStat(ref RoundEndStatsAppendEvent  endEvent, string id, int value, string playerName, int highScore)
    {
        if (highScore <= 0)
        {
            AddStat(ref endEvent, id, value);
            return;
        }

        endEvent.AddLine(Loc.GetString(id + "-top", ("count", value), ("name", playerName), ("value", highScore)));
    }

    private (string name, int value) GetHighScore(Func<PlayerRoundStats, int> selector)
    {
        var bestName = "";
        var bestValue = 0;

        foreach (var (_, stats) in PlayerStats)
        {
            var value = selector(stats);

            if (value <= bestValue)
                continue;

            bestValue = value;
            bestName = stats.UserName;
        }

        return (bestName, bestValue);
    }
}
