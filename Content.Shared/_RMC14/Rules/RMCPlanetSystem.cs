using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Rules;

public sealed class RMCPlanetSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private int _coordinateVariance;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlanetComponent, MapInitEvent>(OnPlanetMapInit);

        Subs.CVar(_config, RMCCVars.RMCPlanetCoordinateVariance, v => _coordinateVariance = v, true);
    }

    private void OnPlanetMapInit(Entity<RMCPlanetComponent> ent, ref MapInitEvent args)
    {
        var x = _random.Next(-_coordinateVariance, _coordinateVariance + 1);
        var y = _random.Next(-_coordinateVariance, _coordinateVariance + 1);
        ent.Comp.Offset = (x, y);
    }

    public bool IsOnPlanet(EntityCoordinates coordinates)
    {
        if (HasComp<RMCPlanetComponent>(_transform.GetGrid(coordinates)))
            return true;

        if (HasComp<RMCPlanetComponent>(_transform.GetMap(coordinates)))
            return true;

        return false;
    }

    public bool IsOnPlanet(MapCoordinates coordinates)
    {
        return IsOnPlanet(_transform.ToCoordinates(coordinates));
    }

    public bool TryGetOffset(MapCoordinates coordinates, out Vector2i offset)
    {
        var entCoords = _transform.ToCoordinates(coordinates);
        if (_transform.GetGrid(entCoords) is { } gridId &&
            TryComp(gridId, out RMCPlanetComponent? gridPlanet))
        {
            offset = gridPlanet.Offset;
            return true;
        }

        if (_transform.GetMap(entCoords) is { } mapId &&
            TryComp(mapId, out RMCPlanetComponent? mapPlanet))
        {
            offset = mapPlanet.Offset;
            return true;
        }

        offset = default;
        return false;
    }
}
