﻿using System.Collections.Immutable;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Rules;

public sealed class RMCPlanetSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private int _coordinateVariance;

    private EntityQuery<RMCPlanetComponent> _rmcPlanetQuery;

    public ImmutableDictionary<string, EntProtoId<RMCPlanetMapPrototypeComponent>> PlanetPaths { get; private set; } =
        ImmutableDictionary<string, EntProtoId<RMCPlanetMapPrototypeComponent>>.Empty;

    public override void Initialize()
    {
        _rmcPlanetQuery = GetEntityQuery<RMCPlanetComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<RMCPlanetComponent, MapInitEvent>(OnPlanetMapInit);

        Subs.CVar(_config, RMCCVars.RMCPlanetCoordinateVariance, v => _coordinateVariance = v, true);

        ReloadPlanets();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPlanets();
    }

    private void OnPlanetMapInit(Entity<RMCPlanetComponent> ent, ref MapInitEvent args)
    {
        var x = _random.Next(-_coordinateVariance, _coordinateVariance + 1);
        var y = _random.Next(-_coordinateVariance, _coordinateVariance + 1);
        ent.Comp.Offset = (x, y);
    }

    public bool IsOnPlanet(EntityCoordinates coordinates)
    {
        if (_rmcPlanetQuery.HasComp(_transform.GetGrid(coordinates)))
            return true;

        if (_rmcPlanetQuery.HasComp(_transform.GetMap(coordinates)))
            return true;

        return false;
    }

    public bool IsOnPlanet(TransformComponent xform)
    {
        if (_rmcPlanetQuery.HasComp(xform.GridUid))
            return true;

        if (_rmcPlanetQuery.HasComp(xform.MapUid))
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

    public bool TryPlanetToCoordinates(Vector2i coordinates, out MapCoordinates mapCoordinates)
    {
        var planets = EntityQueryEnumerator<RMCPlanetComponent>();
        while (planets.MoveNext(out var uid, out var comp))
        {
            var mapId = _transform.GetMapId(uid);
            mapCoordinates = new MapCoordinates(coordinates - comp.Offset, mapId);
            return true;
        }

        mapCoordinates = default;
        return false;
    }

    private void ReloadPlanets()
    {
        var planetPaths = new Dictionary<string, EntProtoId<RMCPlanetMapPrototypeComponent>>();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!entity.TryGetComponent(out RMCPlanetMapPrototypeComponent? planetMapPrototype, _compFactory))
                continue;

            planetPaths[planetMapPrototype.Map.ToString()] = entity.ID;
        }

        PlanetPaths = planetPaths.ToImmutableDictionary();
    }

    public List<RMCPlanet> GetCandidates()
    {
        var players = _player.PlayerCount;
        var candidates = new List<RMCPlanet>();
        foreach (var planet in PlanetPaths.Values)
        {
            if (!_prototypes.TryIndex(planet, out var planetProto) ||
                !planet.TryGet(out var comp, _prototypes, _compFactory))
            {
                continue;
            }

            if (players == 0 || comp.MinPlayers == 0 || players >= comp.MinPlayers)
                candidates.Add(new RMCPlanet(planetProto, comp));
        }

        return candidates;
    }
}
