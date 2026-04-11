using Content.Server.GameTicking;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Stats;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.GameTicking;

namespace Content.Server._RMC14.Stats;

public sealed class StatTrackingSystem : SharedStatTrackingSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (HasComp<RMCAdminSpawnedComponent>(ev.Mob))
            return;

        if (HasComp<XenoParasiteComponent>(ev.Mob))
        {
            TotalPlayerParasites++;
            return;
        }

        if (TryComp(ev.Mob, out XenoComponent? xeno))
        {
            if (xeno.Role == LesserJob)
            {
                TotalLesserXenos++;
                return;
            }

            TotalXenos++;
        }
        else if (HasComp<MarineComponent>(ev.Mob))
            TotalMarines++;
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        TotalMarines = 0;
        TotalMarineDeaths = 0;
        TotalMarinePermaDeaths = 0;
        TotalMarineProjectiles = 0;
        TotalMarineProjectileHits = 0;
        TotalFriendlyFireIncidents = 0;
        TotalLarvaExtractions = 0;
        TotalUsedRequisitionsBudget = 0;
        TotalSupplyDrops = 0;

        TotalXenos = 0;
        TotalXenoDeaths = 0;
        TotalLesserXenos = 0;
        TotalPlayerParasites = 0;
        TotalXenoProjectiles = 0;
        TotalXenoProjectileHits = 0;
        TotalXenoMeleeHits = 0;
        TotalInfected = 0;
        TotalBursts = 0;
    }

    public void AppendRoundEndText(ref RoundEndTextAppendEvent endEvent)
    {
        // Marine stats
        endEvent.AddLine(Loc.GetString("rmc-distress-signal-round-stat-marine-header"));

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-total-marines", TotalMarines);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-marine-deaths", TotalMarineDeaths);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-marine-perma-deaths", TotalMarinePermaDeaths);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-marine-projectiles-fired", TotalMarineProjectiles);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-marine-projectile-hits", TotalMarineProjectileHits);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-friendly-fire-incidents", TotalFriendlyFireIncidents);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-larva-extractions", TotalLarvaExtractions);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-used-requisitions-budget", TotalUsedRequisitionsBudget);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-supply-drops", TotalSupplyDrops);

        endEvent.AddLine(string.Empty);

        // Xeno stats
        endEvent.AddLine(Loc.GetString("rmc-distress-signal-round-stat-xeno-header"));

        AddStat(ref endEvent, "rmc-distress-signal-round-stat-total-xenos", TotalXenos);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-deaths", TotalXenoDeaths);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-lesser-drones", TotalLesserXenos);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-player-parasites", TotalPlayerParasites);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-projectiles-fired", TotalXenoProjectiles);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-projectile-hits", TotalXenoProjectileHits);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-xeno-melee-hits", TotalXenoMeleeHits);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-infected", TotalInfected);
        AddStat(ref endEvent, "rmc-distress-signal-round-stat-bursts", TotalBursts);

        endEvent.AddLine(string.Empty);
    }

    private void AddStat(ref RoundEndTextAppendEvent endEvent, string id, int value)
    {
        endEvent.AddLine(Loc.GetString(id, ("count", value)));
    }
}
