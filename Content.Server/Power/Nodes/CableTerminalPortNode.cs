using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public sealed partial class CableTerminalPortNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan)
        {
            if (!xform.Anchored || grid == null || xform.GridUid is not { } gridUid)
                yield break;

            var mapSystem = entMan.System<SharedMapSystem>();
            var gridEnt = new Entity<MapGridComponent>(gridUid, grid);
            var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Coordinates);

            var nodes = NodeHelpers.GetCardinalNeighborNodes(nodeQuery, gridEnt, gridIndex, mapSystem, includeSameTile: false);
            foreach (var (dir, node) in nodes)
            {
                if (node is CableTerminalNode
                    && dir != Direction.Invalid
                    && xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir().GetOpposite() == dir)
                    yield return node;
            }
        }
    }
}
