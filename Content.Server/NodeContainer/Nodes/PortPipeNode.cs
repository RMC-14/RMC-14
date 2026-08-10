using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    [DataDefinition]
    public sealed partial class PortPipeNode : PipeNode
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

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, gridIndex, mapSystem))
            {
                if (node is PortablePipeNode)
                    yield return node;
            }

            foreach (var node in base.GetReachableNodes(xform, nodeQuery, xformQuery, grid, entMan))
            {
                yield return node;
            }
        }
    }
}
