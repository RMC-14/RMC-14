using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Stats;

public abstract class SharedStatTrackingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    // Marine stats
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

    protected readonly ProtoId<JobPrototype> LesserJob = "CMXenoLesserDrone";
    private readonly EntProtoId<IFFFactionComponent> _marineFaction = "FactionMarine";


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

            TotalMarineDeaths++;
        }
        else if (TryComp(died, out XenoComponent? xeno) && xeno.Role != LesserJob)
            TotalXenoDeaths++;
    }

    public void UpdateProjectileCount(EntityUid projectile)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<XenoProjectileComponent>(projectile))
            TotalXenoProjectiles++;
        else
            TotalMarineProjectiles++;
    }

    public void UpdateProjectileHits(bool isXenoProjectile, EntityUid target, EntityUid? shooter = null)
    {
        if (!_net.IsServer)
            return;

        if (isXenoProjectile)
            TotalXenoProjectileHits++;
        else
        {
            TotalMarineProjectileHits++;

            if (shooter == null)
                return;

            var shooterFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
            RaiseLocalEvent(shooter.Value, ref shooterFactionEvent);

            var targetFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, new());
            RaiseLocalEvent(target, ref targetFactionEvent);

            if (HasComp<MarineComponent>(target) && shooterFactionEvent.Factions.Overlaps(targetFactionEvent.Factions))
                TotalFriendlyFireIncidents++;
        }
    }

    public void UpdateBurstTotal()
    {
        if (!_net.IsServer)
            return;

        TotalBursts++;
    }

    public void UpdateTotalInfected()
    {
        if (!_net.IsServer)
            return;

        TotalInfected++;
    }

    public void UpdateTotalLarvaExtractions()
    {
        if (!_net.IsServer)
            return;

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

    public void UpdateXenoMeleeHitTotal()
    {
        if (!_net.IsServer)
            return;

        TotalXenoMeleeHits++;
    }
}
