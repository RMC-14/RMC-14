using System.Diagnostics.CodeAnalysis;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Placement;

public sealed class RMCPlacementSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <summary>
    ///     Tries to find an anchored entity that blocks placement.
    /// </summary>
    public bool TryFindBlocker(EntityCoordinates coordinates, IReadOnlyList<RMCPlacementRestriction> restrictions, [NotNullWhen(true)] out EntityUid? blocker, EntityUid? ignored = null)
    {
        blocker = null;
        if (restrictions.Count == 0)
            return false;

        var grid = _transform.GetGrid(coordinates);
        if (!TryComp(grid, out MapGridComponent? mapGrid))
            return false;

        var position = _map.LocalToTile(grid.Value, mapGrid, coordinates);
        foreach (var restriction in restrictions)
        {
            if (restriction.Radius < 0)
                continue;

            var radius = restriction.Radius;
            var area = new Box2(position.X - radius, position.Y - radius, position.X + radius + 1, position.Y + radius + 1);

            foreach (var anchored in _map.GetLocalAnchoredEntities(grid.Value, mapGrid, area))
            {
                if (anchored == ignored || !_whitelist.IsValid(restriction.Blacklist, anchored))
                    continue;

                blocker = anchored;
                return true;
            }
        }

        return false;
    }
}
