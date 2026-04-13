using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.FixedPoint;
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
    protected FixedPoint2 TotalDamageReceived;
    protected FixedPoint2 TotalDamageHealed;
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
    protected FixedPoint2 TotalXenoDamageReceived;
    protected FixedPoint2 TotalXenoDamageHealed;
    protected int TotalInfected;
    protected int TotalBursts;

    protected Dictionary<NetUserId, PlayerRoundStats> PlayerStats = new();
    protected TimeSpan RoundStartTime;

    protected void ModifyStats(NetUserId userId, string? name, Func<PlayerRoundStats, PlayerRoundStats> modify)
    {
        PlayerStats.TryGetValue(userId, out var stats);

        if (string.IsNullOrEmpty(stats.UserName) && name != null)
            stats.UserName = name;

        stats = modify(stats);
        PlayerStats[userId] = stats;
    }

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
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineDeath);

            TotalMarineDeaths++;
        }
        else if (TryComp(died, out XenoComponent? xeno) && xeno.Role != LesserJob)
        {
            if (TryComp(died, out ActorComponent? actor))
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.XenoDeath);

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
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.XenoProjectileFired);
            else
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineProjectileFired);
        }

        if (isXeno)
            TotalXenoProjectiles++;
        else
            TotalMarineProjectiles++;
    }

    public void UpdateProjectileHits(EntityUid target, EntityUid? shooter = null)
    {
        if (!_net.IsServer)
            return;

        if (!_mobState.IsAlive(target) && !_mobState.IsCritical(target))
            return;

        if (HasComp<RMCAdminSpawnedComponent>(target))
            return;

        if (TryComp(shooter, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineProjectileHit);

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
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineFriendlyFire);

            TotalFriendlyFireIncidents++;
        }
    }

    public void UpdateXenoProjectileHits(EntityUid target, EntityUid? shooter = null)
    {
        if (!_net.IsServer)
            return;

        if (!_mobState.IsAlive(target) && !_mobState.IsCritical(target))
            return;

        if (HasComp<RMCAdminSpawnedComponent>(target))
            return;

        if (TryComp(shooter, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.XenoProjectileHit);

        TotalXenoProjectileHits++;
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
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineLarvaExtraction);

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
                ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.XenoMeleeHit);

            TotalXenoMeleeHits++;
        }
    }

    public void UpdateDamageReceived(EntityUid damaged, FixedPoint2 amount, EntityUid? origin)
    {
        if (!_net.IsServer)
            return;

        var isDamage = amount >= 0;

        if (isDamage && !_mobState.IsAlive(damaged) && !_mobState.IsCritical(damaged) || damaged == origin)
            return;

        if (HasComp<RMCAdminSpawnedComponent>(damaged))
            return;

        if (HasComp<XenoComponent>(damaged))
        {
            if (isDamage)
            {
                if (TryComp(damaged, out ActorComponent? actor))
                {
                    ModifyStats(
                        actor.PlayerSession.UserId,
                        actor.PlayerSession.Data.UserName,
                        stats => PlayerRoundStatModifications.XenoDamageReceived(stats, amount)
                    );
                }

                TotalXenoDamageReceived += amount;
            }
            else if (TryComp(origin, out ActorComponent? actor))
            {
                {
                    ModifyStats(
                        actor.PlayerSession.UserId,
                        actor.PlayerSession.Data.UserName,
                        stats => PlayerRoundStatModifications.XenoDamageHealed(stats, -amount)
                    );
                }

                TotalXenoDamageHealed += -amount;
            }
        }
        else if (HasComp<MarineComponent>(damaged))
        {
            if (isDamage)
            {
                if (TryComp(damaged, out ActorComponent? actor))
                {
                    ModifyStats(
                        actor.PlayerSession.UserId,
                        actor.PlayerSession.Data.UserName,
                        stats => PlayerRoundStatModifications.DamageReceived(stats, amount)
                    );
                }

                TotalDamageReceived += amount;
            }
            else if (TryComp(origin, out ActorComponent? actor))
            {
                ModifyStats(
                    actor.PlayerSession.UserId,
                    actor.PlayerSession.Data.UserName,
                    stats => PlayerRoundStatModifications.DamageHealed(stats, -amount)
                );

                TotalDamageHealed += -amount;
            }
        }
    }
}

public struct PlayerRoundStats
{
    public string UserName;

    public MarineRoundStats Marine;
    public XenoRoundStats Xeno;
}

public struct MarineRoundStats
{
    public int TotalMarineDeaths;
    public FixedPoint2 TotalDamageReceived;
    public FixedPoint2 TotalDamageHealed;
    public int TotalProjectiles;
    public int TotalProjectileHits;
    public int TotalFriendlyFireIncidents;
    public int TotalLarvaExtractions;
}

public struct XenoRoundStats
{
    public int TotalDeaths;
    public FixedPoint2 TotalDamageReceived;
    public FixedPoint2 TotalDamageHealed;
    public int TotalLesserDroneSpawns;
    public int TotalParasiteSpawns;
    public int TotalProjectiles;
    public int TotalProjectileHits;
    public int TotalMeleeHits;
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

#region Stat Modifications
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
