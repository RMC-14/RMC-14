using Content.Shared._RMC14.Admin;
using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Stats;

public abstract partial class SharedStatTrackingSystem
{
    protected readonly ProtoId<JobPrototype> LesserJob = "CMXenoLesserDrone";

    // Global Xeno Stats
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

    private void UpdateXenoDeathCount(EntityUid died)
    {
        if (TryComp(died, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.XenoDeath);

        TotalXenoDeaths++;
    }

    private void UpdateXenODamage(EntityUid damaged, EntityUid? origin, FixedPoint2 amount, bool isDamage)
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
