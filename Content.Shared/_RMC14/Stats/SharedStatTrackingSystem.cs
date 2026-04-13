using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Stats;

public abstract class SharedStatTrackingSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected readonly ProtoId<JobPrototype> LesserJob = "CMXenoLesserDrone";
    private readonly EntProtoId<IFFFactionComponent> _marineFaction = "FactionMarine";
    private readonly TimeSpan _roundStartTrackingDelay = TimeSpan.FromMinutes(1);

    // Marine Stats
    protected int TotalMarines;
    protected int TotalMarineDeaths;
    protected int TotalMarinePermaDeaths;
    protected int TotalMarineProjectiles;
    protected int TotalMarineProjectileHits;
    protected int TotalFriendlyFireIncidents;
    protected int TotalLarvaExtractions;
    protected int TotalUsedRequisitionsBudget;
    protected int TotalSupplyDrops;

    // Xeno Stats
    protected int TotalXenos;
    protected int TotalXenoDeaths;
    protected int TotalLesserXenos;
    protected int TotalPlayerParasites;
    protected int TotalXenoProjectiles;
    protected int TotalXenoProjectileHits;
    protected int TotalXenoMeleeHits;
    protected int TotalInfected;
    protected int TotalBursts;

    protected Dictionary<NetUserId, PlayerRoundStats> PlayerStats = new ();
    protected TimeSpan RoundStartTime;

    public void UpdateDeathCount(EntityUid died)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(died) || HasComp<XenoParasiteComponent>(died))
            return;

        if (HasComp<MarineComponent>(died))
        {
            var diedFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
            RaiseLocalEvent(died, ref diedFactionEvent);

            if (!diedFactionEvent.Factions.Contains(_marineFaction))
                return;

            if (TryComp(died, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.MarineDeath);

            TotalMarineDeaths++;
        }
        else if (TryComp(died, out XenoComponent? xeno) && xeno.Role != LesserJob)
        {
            if (TryComp(died, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.XenoDeath);

            TotalXenoDeaths++;
        }
    }

    public void UpdateProjectileCount(EntityUid projectile, EntityUid? shooter)
    {
        if (!_net.IsServer)
            return;

        var isXeno = HasComp<XenoProjectileComponent>(projectile);

        if (TryComp(shooter, out ActorComponent? actor))
        {
            if (isXeno)
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.XenoProjectileFired);
            else
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.MarineProjectileFired);
        }

        if (isXeno)
            TotalXenoProjectiles++;
        else
            TotalMarineProjectiles++;
    }

    public void UpdateProjectileHits(bool isXenoProjectile, EntityUid target, EntityUid? shooter = null)
    {
        if (!_net.IsServer)
            return;

        if (!_mobState.IsAlive(target) && !_mobState.IsCritical(target))
            return;

        if (isXenoProjectile)
        {
            if (TryComp(shooter, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.XenoProjectileHit);

            TotalXenoProjectileHits++;
        }
        else
        {
            if (TryComp(shooter, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.MarineProjectileHit);

            TotalMarineProjectileHits++;

            if (shooter == null)
                return;

            var shooterFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
            RaiseLocalEvent(shooter.Value, ref shooterFactionEvent);

            var targetFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
            RaiseLocalEvent(target, ref targetFactionEvent);

            if (HasComp<MarineComponent>(target) && shooterFactionEvent.Factions.Overlaps(targetFactionEvent.Factions))
            {
                if (actor != null)
                    ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.MarineFriendlyFire);

                TotalFriendlyFireIncidents++;
            }
        }
    }

    public void UpdateBurstTotal()
    {
        if (!_net.IsServer)
            return;

        if (RoundStartTime + _roundStartTrackingDelay > Timing.CurTime) // Don't count round start bursts
            return;

        TotalBursts++;
    }

    public void UpdateTotalInfected()
    {
        if (!_net.IsServer)
            return;

        if (RoundStartTime + _roundStartTrackingDelay > Timing.CurTime) // Don't count round start infections
            return;

        TotalInfected++;
    }

    public void UpdateTotalLarvaExtractions(EntityUid surgeon)
    {
        if (!_net.IsServer)
            return;

        if (TryComp(surgeon, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.MarineLarvaExtraction);

        TotalLarvaExtractions++;
    }

    public void UpDateUsedRequisitionsBudgetTotal(int amount)
    {
        if (!_net.IsServer)
            return;

        TotalUsedRequisitionsBudget += amount;
    }

    public void UpdateSupplyDropCount()
    {
        if (!_net.IsServer)
            return;

        TotalSupplyDrops++;
    }

    public void UpdateMarinePermaDeathTotal(EntityUid died)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(died))
            return;

        var diedFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
        RaiseLocalEvent(died, ref diedFactionEvent);

        if (!diedFactionEvent.Factions.Contains(_marineFaction))
            return;

        TotalMarinePermaDeaths++;
    }

    public void UpdateXenoMeleeHitTotal(IReadOnlyList<EntityUid> targets, EntityUid xeno)
    {
        if (!_net.IsServer)
            return;

        foreach (var target in targets)
        {
            if (!_mobState.IsAlive(target) || !_mobState.IsCritical(target))
                continue;

            if (TryComp(xeno, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatOperations.XenoMeleeHit);

            TotalXenoMeleeHits++;
        }
    }

    protected void ModifyStats(NetUserId userId, string? name, Func<PlayerRoundStats, PlayerRoundStats> mutate)
    {
        PlayerStats.TryGetValue(userId, out var stats);

        if (string.IsNullOrEmpty(stats.UserName) && name != null)
            stats.UserName = name;

        stats = mutate(stats);
        PlayerStats[userId] = stats;
    }
}

public sealed class RoundEndStatsAppendEvent
{
    private bool _doNewLine;

    public string Text { get; private set; } = string.Empty;

    public void AddLine(string text)
    {
        if (_doNewLine)
            Text += "\n";

        Text += text;
        _doNewLine = true;
    }
}

[DataRecord]
public struct PlayerRoundStats
{
    public string UserName;

    // Marine Stats
    public int TotalMarineDeaths;
    public int TotalProjectiles;
    public int TotalProjectileHits;
    public int TotalFriendlyFireIncidents;
    public int TotalLarvaExtractions;

    // Xeno Stats
    public int TotalXenoDeaths;
    public int TotalLesserDroneSpawns;
    public int TotalParasiteSpawns;
    public int TotalXenoProjectiles;
    public int TotalXenoProjectileHits;
    public int TotalXenoMeleeHits;
}

#region Stat Increase
public static class PlayerRoundStatOperations
{
    // Marine stats
    public static PlayerRoundStats MarineDeath(PlayerRoundStats stats)
    {
        stats.TotalMarineDeaths++;
        return stats;
    }

    public static PlayerRoundStats MarineProjectileFired(PlayerRoundStats stats)
    {
        stats.TotalProjectiles++;
        return stats;
    }

    public static PlayerRoundStats MarineProjectileHit(PlayerRoundStats stats)
    {
        stats.TotalProjectileHits++;
        return stats;
    }

    public static PlayerRoundStats MarineFriendlyFire(PlayerRoundStats stats)
    {
        stats.TotalFriendlyFireIncidents++;
        return stats;
    }

    public static PlayerRoundStats MarineLarvaExtraction(PlayerRoundStats stats)
    {
        stats.TotalLarvaExtractions++;
        return stats;
    }

    // Xeno stats

    public static PlayerRoundStats XenoDeath(PlayerRoundStats stats)
    {
        stats.TotalXenoDeaths++;
        return stats;
    }

    public static PlayerRoundStats XenoProjectileFired(PlayerRoundStats stats)
    {
        stats.TotalXenoProjectiles++;
        return stats;
    }

    public static PlayerRoundStats XenoProjectileHit(PlayerRoundStats stats)
    {
        stats.TotalXenoProjectileHits++;
        return stats;
    }

    public static PlayerRoundStats XenoMeleeHit(PlayerRoundStats stats)
    {
        stats.TotalXenoMeleeHits++;
        return stats;
    }

    public static PlayerRoundStats LesserDroneSpawn(PlayerRoundStats stats)
    {
        stats.TotalLesserDroneSpawns++;
        return stats;
    }

    public static PlayerRoundStats ParasiteSpawn(PlayerRoundStats stats)
    {
        stats.TotalParasiteSpawns++;
        return stats;
    }
}
# endregion
