using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Rules;

public sealed class RMCPlanetSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private int _coordinateVariance;
    private float _hijackSongGain;

    private EntityQuery<RMCPlanetComponent> _rmcPlanetQuery;

    public ImmutableDictionary<string, EntProtoId<RMCPlanetMapPrototypeComponent>> PlanetPaths { get; private set; } =
        ImmutableDictionary<string, EntProtoId<RMCPlanetMapPrototypeComponent>>.Empty;

    public override void Initialize()
    {
        _rmcPlanetQuery = GetEntityQuery<RMCPlanetComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<RMCPlanetComponent, MapInitEvent>(OnPlanetMapInit);

        SubscribeLocalEvent<RMCHijackSongComponent, ComponentStartup>(OnHijackSongStartup);

        Subs.CVar(_config, RMCCVars.RMCPlanetCoordinateVariance, v => _coordinateVariance = v, true);
        Subs.CVar(_config, RMCCVars.VolumeGainHijackSong, SetVolumeHijack, true);

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

        var ev = new RMCPlanetAddedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnHijackSongStartup(Entity<RMCHijackSongComponent> ent, ref ComponentStartup args)
    {
        if (TryComp(ent, out AudioComponent? audio))
            audio.Gain = _hijackSongGain;
    }

    private void SetVolumeHijack(float gain)
    {
        _hijackSongGain = gain;
        var query = AllEntityQuery<RMCHijackSongComponent, AudioComponent>();
        while (query.MoveNext(out _, out _, out var audio))
        {
#pragma warning disable RA0002
            audio.Params = audio.Params with { Volume = SharedAudioSystem.GainToVolume(gain) };
#pragma warning restore RA0002
        }
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

    public List<RMCPlanet> GetAllPlanets()
    {
        var candidates = new List<RMCPlanet>();
        foreach (var planet in PlanetPaths.Values)
        {
            if (!_prototypes.TryIndex(planet, out var planetProto) ||
                !planet.TryGet(out var comp, _prototypes, _compFactory))
            {
                continue;
            }

            candidates.Add(new RMCPlanet(planetProto, comp));
        }

        return candidates;
    }

    public List<RMCPlanet> GetAllPlanetsInRotation()
    {
        return GetAllPlanets().Where(p => p.Comp.InRotation).ToList();
    }

    public List<RMCPlanet> GetCandidatesInRotation()
    {
        var candidates = GetAllPlanetsInRotation();
        var players = _player.PlayerCount;
        if (players == 0)
            return candidates;

        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var comp = candidates[i].Comp;

            if ((comp.MinPlayers != 0 && players < comp.MinPlayers) ||
                (comp.MaxPlayers != 0 && players > comp.MaxPlayers))
            {
                candidates.RemoveAt(i);
            }
        }

        return candidates;
    }

    public MapId? Load(ResPath path)
    {
        var options = new DeserializationOptions { InitializeMaps = true };
        if (!_mapLoader.TryLoadMap(path, out var map, out _, options))
            return null;

        foreach (var entity in EntityManager.AllEntities<RMCPlanetComponent>())
        {
            RemComp<RMCPlanetComponent>(entity);
        }

        foreach (var entity in EntityManager.AllEntities<TacticalMapComponent>())
        {
            RemComp<TacticalMapComponent>(entity);
        }

        EnsureComp<RMCPlanetComponent>(map.Value);
        EnsureComp<TacticalMapComponent>(map.Value);
        _rmcPower.RecalculatePower();
        return map.Value.Comp.MapId;
    }
}
