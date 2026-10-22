using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Chemistry.TileReactions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCNeutralizeTileReaction : ITileReaction
{
    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager,
        List<ReagentData>? data)
    {
        if (reactVolume <= FixedPoint2.Zero || tile.Tile.IsEmpty)
            return FixedPoint2.Zero;

        var lookup = entityManager.System<EntityLookupSystem>();
        var acid = entityManager.System<SharedXenoAcidSystem>();
        foreach (var uid in lookup.GetLocalEntitiesIntersecting(tile.GridUid, tile.GridIndices))
        {
            if (entityManager.HasComponent<TileFireComponent>(uid))
            {
                entityManager.QueueDeleteEntity(uid);
                continue;
            }

            if (acid.IsMelted(uid))
                acid.RemoveAcid(uid);
        }

        return FixedPoint2.Zero;
    }
}
