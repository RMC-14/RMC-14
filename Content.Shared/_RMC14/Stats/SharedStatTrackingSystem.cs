using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Stats;

public abstract partial class SharedStatTrackingSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly TimeSpan _roundStartTrackingDelay = TimeSpan.FromMinutes(1);

    protected readonly Dictionary<NetUserId, PlayerRoundStats> PlayerStats = new();
    protected TimeSpan RoundStartTime;

    protected void ModifyStats(NetUserId userId, string? name, Func<PlayerRoundStats, PlayerRoundStats> modify)
    {
        PlayerStats.TryGetValue(userId, out var stats);

        if (string.IsNullOrEmpty(stats.UserName) && name != null)
            stats.UserName = name;

        stats = modify(stats);
        PlayerStats[userId] = stats;
    }

    public void UpdateDeathCount(EntityUid died, EntityUid? cause)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(died) || HasComp<XenoParasiteComponent>(died))
            return;

        if (HasComp<MarineComponent>(died))
        {
            UpdateMarineDeathCount(died, cause);
        }
        else if (TryComp(died, out XenoComponent? xeno) && xeno.Role != LesserJob)
        {
            UpdateXenoDeathCount(died);
        }
    }

    public void UpdateProjectileCount(EntityUid projectile, EntityUid? shooter)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(shooter))
            return;

        var isXeno = HasComp<XenoProjectileComponent>(projectile);

        if (TryComp(shooter, out ActorComponent? actor))
        {
            Func<PlayerRoundStats, PlayerRoundStats> adjustedStat = isXeno
                ? PlayerRoundStatModifications.XenoProjectileFired
                : PlayerRoundStatModifications.MarineProjectileFired;

            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, adjustedStat);
        }

        if (isXeno)
            TotalXenoProjectiles++;
        else
            TotalMarineProjectiles++;
    }

    public void UpdateDamageReceived(EntityUid damaged, FixedPoint2 amount, EntityUid? origin)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(damaged))
            return;

        var isDamage = amount >= 0;

        if (damaged == origin) // Don't track damaging/healing yourself
            return;

        if (isDamage && !_mobState.IsAlive(damaged) && !_mobState.IsCritical(damaged))
            return;

        if (HasComp<XenoComponent>(damaged))
        {
            UpdateXenODamage(damaged, origin, amount, isDamage);
        }
        else if (HasComp<MarineComponent>(damaged))
        {
            UpdateMarineDamage(damaged, origin, amount, isDamage);
        }
    }
}

public struct PlayerRoundStats
{
    public string UserName;

    public MarineRoundStats Marine;
    public XenoRoundStats Xeno;
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

#region Player Stat Modifications
public static class PlayerRoundStatModifications
{
    // Marine stats
    public static PlayerRoundStats MarineDeath(PlayerRoundStats stats)
    {
        stats.Marine.TotalMarineDeaths++;
        return stats;
    }

    public static PlayerRoundStats DamageReceived(PlayerRoundStats stats, FixedPoint2 amount)
    {
        stats.Marine.TotalDamageReceived += amount;
        return stats;
    }

    public static PlayerRoundStats DamageHealed(PlayerRoundStats stats, FixedPoint2 amount)
    {
        stats.Marine.TotalDamageHealed += amount;
        return stats;
    }

    public static PlayerRoundStats MarineProjectileFired(PlayerRoundStats stats)
    {
        stats.Marine.TotalProjectiles++;
        return stats;
    }

    public static PlayerRoundStats MarineProjectileHit(PlayerRoundStats stats)
    {
        stats.Marine.TotalProjectileHits++;
        return stats;
    }

    public static PlayerRoundStats MarineFriendlyFire(PlayerRoundStats stats)
    {
        stats.Marine.TotalFriendlyFireIncidents++;
        return stats;
    }

    public static PlayerRoundStats MarineFriendlyFireKill(PlayerRoundStats stats)
    {
        stats.Marine.TotalFriendlyFireKills++;
        return stats;
    }

    public static PlayerRoundStats MarineLarvaExtraction(PlayerRoundStats stats)
    {
        stats.Marine.TotalLarvaExtractions++;
        return stats;
    }

    // Xeno stats

    public static PlayerRoundStats XenoDeath(PlayerRoundStats stats)
    {
        stats.Xeno.TotalDeaths++;
        return stats;
    }

    public static PlayerRoundStats XenoDamageReceived(PlayerRoundStats stats, FixedPoint2 amount)
    {
        stats.Xeno.TotalDamageReceived += amount;
        return stats;
    }

    public static PlayerRoundStats XenoDamageHealed(PlayerRoundStats stats, FixedPoint2 amount)
    {
        stats.Xeno.TotalDamageHealed += amount;
        return stats;
    }

    public static PlayerRoundStats XenoProjectileFired(PlayerRoundStats stats)
    {
        stats.Xeno.TotalProjectiles++;
        return stats;
    }

    public static PlayerRoundStats XenoProjectileHit(PlayerRoundStats stats)
    {
        stats.Xeno.TotalProjectileHits++;
        return stats;
    }

    public static PlayerRoundStats XenoMeleeHit(PlayerRoundStats stats)
    {
        stats.Xeno.TotalMeleeHits++;
        return stats;
    }

    public static PlayerRoundStats LesserDroneSpawn(PlayerRoundStats stats)
    {
        stats.Xeno.TotalLesserDroneSpawns++;
        return stats;
    }

    public static PlayerRoundStats ParasiteSpawn(PlayerRoundStats stats)
    {
        stats.Xeno.TotalParasiteSpawns++;
        return stats;
    }
}
# endregion
