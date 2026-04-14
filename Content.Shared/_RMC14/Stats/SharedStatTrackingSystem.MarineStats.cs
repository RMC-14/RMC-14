using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Stats;

public abstract partial class SharedStatTrackingSystem
{
    private readonly EntProtoId<IFFFactionComponent> _marineFaction = "FactionMarine";

    // Global Marine Stats
    protected int TotalMarines;
    protected int TotalMarineDeaths;
    protected int TotalMarinePermaDeaths;
    protected int TotalMarineProjectiles;
    protected int TotalMarineProjectileHits;
    protected int TotalFriendlyFireIncidents;
    protected int TotalFriendlyFireKills;
    protected FixedPoint2 TotalDamageReceived;
    protected FixedPoint2 TotalDamageHealed;
    protected int TotalLarvaExtractions;
    protected int TotalUsedRequisitionsBudget;
    protected int TotalSupplyDrops;

    public void UpdateProjectileHits(EntityUid projectile, EntityUid target, EntityUid? shooter = null)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<XenoProjectileComponent>(projectile))
            return;

        if (HasComp<RMCAdminSpawnedComponent>(target))
            return;

        if (!_mobState.IsAlive(target) && !_mobState.IsCritical(target))
            return;

        if (TryComp(shooter, out ActorComponent? actor))
        {
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineProjectileHit);
            CheckFriendlyFire((shooter.Value, actor), target);
        }

        TotalMarineProjectileHits++;
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

    public void UpdateTotalLarvaExtractions(EntityUid surgeon)
    {
        if (!_net.IsServer)
            return;

        if (TryComp(surgeon, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineLarvaExtraction);

        TotalLarvaExtractions++;
    }

    private void UpdateMarineDeathCount(EntityUid died, EntityUid? cause)
    {
        var diedFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
        RaiseLocalEvent(died, ref diedFactionEvent);

        if (!diedFactionEvent.Factions.Contains(_marineFaction))
            return;

        if (TryComp(died, out ActorComponent? actor))
            ModifyStats(actor.PlayerSession.UserId, actor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineDeath);

        if (cause != null)
            UpdateFriendlyFireKills(cause.Value, died);

        TotalMarineDeaths++;
    }

    private void CheckFriendlyFire(Entity<ActorComponent> shooter, EntityUid target)
    {
        if (!IsFriendlyFire(shooter, target))
            return;

        ModifyStats(shooter.Comp.PlayerSession.UserId, shooter.Comp.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineFriendlyFire);
        TotalFriendlyFireIncidents++;
    }

    private bool IsFriendlyFire(Entity<ActorComponent> shooter, EntityUid target)
    {
        var shooterFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
        RaiseLocalEvent(shooter, ref shooterFactionEvent);

        var targetFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
        RaiseLocalEvent(target, ref targetFactionEvent);

        return shooterFactionEvent.Factions.Overlaps(targetFactionEvent.Factions);
    }

    private void UpdateMarineDamage(EntityUid damaged, EntityUid? origin, FixedPoint2 amount, bool isDamage)
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

    private void UpdateFriendlyFireKills(EntityUid killer, EntityUid died)
    {
        if (!TryComp(killer, out ActorComponent? killerActor))
            return;

        if (!IsFriendlyFire((killer, killerActor), died))
            return;

        ModifyStats(killerActor.PlayerSession.UserId, killerActor.PlayerSession.Data.UserName, PlayerRoundStatModifications.MarineFriendlyFireKill);
        TotalFriendlyFireKills++;
    }
}

public struct MarineRoundStats
{
    public int TotalMarineDeaths;
    public FixedPoint2 TotalDamageReceived;
    public FixedPoint2 TotalDamageHealed;
    public int TotalProjectiles;
    public int TotalProjectileHits;
    public int TotalFriendlyFireIncidents;
    public int TotalFriendlyFireKills;
    public int TotalLarvaExtractions;
}
