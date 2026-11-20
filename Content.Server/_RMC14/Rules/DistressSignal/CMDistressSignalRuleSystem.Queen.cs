using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Database;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private void OnHiveChanged(Entity<HiveMemberComponent> ent, ref HiveChangedEvent args)
    {
        if (!_queenBuildingBoostEnabled)
            return;

        if (!HasComp<XenoEvolutionGranterComponent>(ent))
            return;

        if (args.Hive == null)
            return;

        var comp = TryGetActiveRule();
        if (comp == null || comp.QueenBoostRemoved)
            return;

        var withinBoostPeriod = comp.StartTime == null ||
                            (Timing.CurTime - comp.StartTime < _queenBoostDuration);

        if (withinBoostPeriod)
        {
            GiveQueenBoost(ent);
        }
    }

    /// <summary>
    /// Applies a building boost to the queen, increasing construction speed and remote range.
    /// </summary>
    private void GiveQueenBoost(EntityUid queen)
    {
        _xenoConstruction.GiveQueenBoost(queen, _queenBoostSpeedMultiplier, _queenBoostRemoteRange);

        _adminLog.Add(LogType.RMCXenoSpawn, $"Queen {ToPrettyString(queen):queen} received building boost");
    }

    private void OnXenoComponentInit(Entity<XenoComponent> ent, ref ComponentInit args)
    {
        if (!_queenBuildingBoostEnabled)
            return;

        var comp = TryGetActiveRule();
        if (comp == null)
            return;

        if (!TryComp(ent.Owner, out MetaDataComponent? metaData) ||
            metaData.EntityPrototype?.ID != comp.QueenEnt.Id)
            return;

        var withinBoostPeriod = comp.StartTime == null ||
                            (Timing.CurTime - comp.StartTime < _queenBoostDuration);

        if (withinBoostPeriod)
        {
            GiveQueenBoost(ent.Owner);
        }
    }

    /// <summary>
    /// Removes all active queen building boosts from Queens on the map.
    /// </summary>
    private void RemoveQueenBuildingBoosts()
    {
        var queens = EntityQueryEnumerator<QueenBuildingBoostComponent, XenoEvolutionGranterComponent>();
        while (queens.MoveNext(out var queen, out _, out _))
        {
            _xenoConstruction.RemoveQueenBoost(queen);
        }
    }
}
