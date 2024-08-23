using Content.Shared._RMC14.Map;
using Content.Shared.Coordinates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

    private EntityQuery<AreaComponent> _areaQuery;

    public override void Initialize()
    {
        _areaQuery = GetEntityQuery<AreaComponent>();
    }

    public bool TryGetArea(EntityCoordinates coordinates, out Entity<AreaComponent> area)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (!_areaQuery.TryComp(uid, out var comp))
                continue;

            area = (uid, comp);
            return true;
        }

        area = default;
        return false;
    }

    public bool TryGetArea(EntityUid coordinates, out Entity<AreaComponent> area)
    {
        return TryGetArea(coordinates.ToCoordinates(), out area);
    }

    public bool BioscanBlocked(EntityUid coordinates, out Entity<AreaComponent>? area)
    {
        area = null;
        if (!TryGetArea(coordinates, out var coordinatesArea))
            return false;

        area = coordinatesArea;
        return coordinatesArea.Comp.AvoidBioscan;
    }

    public bool CanCAS(EntityCoordinates coordinates)
    {
        if (!TryGetArea(coordinates, out var area))
            return false;

        return area.Comp.CAS;
    }
}
