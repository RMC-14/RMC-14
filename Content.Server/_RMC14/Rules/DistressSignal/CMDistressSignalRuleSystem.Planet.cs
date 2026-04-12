using System.Linq;
using System.Text;
using Content.Server._RMC14.MapInsert;
using Content.Server.Voting;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{

    /// <summary>
    /// Loads the planet map, initializes ambient light, miasma, and tunnel systems.
    /// Sets up the xeno gameplay area for the current round.
    /// </summary>
    /// <returns>True if the map was successfully loaded, false otherwise.</returns>
    private bool SpawnXenoMap(Entity<CMDistressSignalRuleComponent> rule)
    {
        var planet = SelectRandomPlanet();
        _lastPlanetMaps.Enqueue(planet.Proto.ID);
        while (_lastPlanetMaps.Count > 0 && _lastPlanetMaps.Count > _mapVoteExcludeLast)
        {
            _lastPlanetMaps.Dequeue();
        }

        if (!_mapLoader.TryLoadMap(planet.Comp.Map, out var mapNullable, out var grids))
            return false;

        var map = mapNullable.Value;
        EnsureComp<RMCPlanetComponent>(map);
        EnsureComp<TacticalMapComponent>(map);

        if (grids.Count == 0)
            return false;

        if (grids.Count > 1)
            Log.Error("Multiple planet-side grids found");

        rule.Comp.XenoMap = grids.First();
        _mapSystem.InitializeMap((map, map));

        ActiveNightmareScenario = string.Empty;
        if (SelectedPlanetMap?.Comp.NightmareScenarios != null)
        {
            ActiveNightmareScenario = _mapInsert.SelectMapScenario(SelectedPlanetMap.Value.Comp.NightmareScenarios);
        }
        var mapInsertQuery = EntityQueryEnumerator<MapInsertComponent>();
        while (mapInsertQuery.MoveNext(out var uid, out var mapInsert))
        {
            _mapInsert.ProcessMapInsert((uid, mapInsert));
        }

        if (_landingZoneMiasmaEnabled &&
            rule.Comp.LandingZoneGas is { } gas &&
            TryComp(rule.Comp.XenoMap, out AreaGridComponent? areaGrid))
        {
            foreach (var (indices, areaProto) in areaGrid.Areas)
            {
                if (areaProto.TryGet(out var area, _prototypes, _compFactory) &&
                    area.LandingZone)
                {
                    var coordinates = _mapSystem.ToCoordinates(rule.Comp.XenoMap.Value, indices);
                    Spawn(gas, coordinates);
                }
            }
        }

        var tunnels = EntityQueryEnumerator<XenoTunnelComponent>();
        while (tunnels.MoveNext(out var uid, out _))
        {
            RemCompDeferred<DeletedByWeedKillerComponent>(uid);
        }

        var xenoMap = rule.Comp.XenoMap.Value;
        var rmcAmbientComp = EnsureComp<RMCAmbientLightComponent>(xenoMap);
        var rmcAmbientEffectComp = EnsureComp<RMCAmbientLightEffectsComponent>(xenoMap);
        var colorSequence = _rmcAmbientLight.ProcessPrototype(rmcAmbientEffectComp.Sunset);
        _rmcAmbientLight.SetColor((xenoMap, rmcAmbientComp), colorSequence, _sunsetDuration);

        return true;
    }

    private RMCPlanet SelectRandomPlanet()
    {
        if (SelectedPlanetMap != null)
            return SelectedPlanetMap.Value;

        var planet = _random.Pick(_rmcPlanet.GetCandidatesInRotation());
        SelectedPlanetMap = planet;
        return planet;
    }

    private void ResetSelectedPlanet()
    {
        SelectedPlanetMap = null;
    }

    /// <summary>
    /// Forces the selected planet for the current round, overriding random selection or voting.
    /// </summary>
    /// <param name="planet">The planet to use for this round.</param>
    public void SetPlanet(RMCPlanet planet)
    {
        SelectedPlanetMap = planet;
    }

    /// <summary>
    /// Starts a voting session for selecting the next planet map, supporting carryover votes from previous rounds.
    /// </summary>
    private void StartPlanetVote()
    {
        if (!_config.GetCVar(RMCCVars.RMCPlanetMapVote))
            return;

        var planets = _rmcPlanet.GetCandidatesInRotation();
        if (!_useCarryoverVoting)
        {
            foreach (var planet in planets)
            {
                _carryoverVotes[planet.Proto.ID] = 0;
            }
        }

        planets.RemoveAll(p => _lastPlanetMaps.Contains(p.Proto.ID));

        var options = new List<(string text, object data)>();
        foreach (var planet in planets)
        {
            var name = planet.Proto.Name;
            var votes = _carryoverVotes.GetValueOrDefault(planet.Proto.ID);
            if (votes > 0)
                name = $"{name} [+{votes}]";

            options.Add((name, planet.Comp.Map.ToString()));
        }

        var vote = new VoteOptions
        {
            Title = Loc.GetString("rmc-distress-signal-next-map-title"),
            Options = options,
            Duration = TimeSpan.FromMinutes(2),
        };
        vote.SetInitiatorOrServer(null);

        _currentVote = _voteManager.CreateVote(vote);
        _currentVote.OnFinished += (_, args) =>
        {
            _currentVote = null;
            RMCPlanet picked;

            var adjustedVotes = planets
                .Zip(args.Votes, (planet, newVotes) => (
                    planet,
                    newVotes,
                    totalVotes: newVotes + _carryoverVotes.GetValueOrDefault(planet.Proto.ID)
                ))
                .ToList();
            var maxVotes = adjustedVotes.Max(v => v.totalVotes);
            var winningMaps = adjustedVotes
                .Where(v => v.totalVotes == maxVotes)
                .Select(v => v.planet)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(Loc.GetString("rmc-distress-signal-next-map-header"));
            foreach (var result in adjustedVotes)
            {
                sb.AppendLine(Loc.GetString(result.newVotes > 0
                    ? "rmc-distress-signal-next-map-votes-new"
                    : "rmc-distress-signal-next-map-votes",
                    ("map", result.planet.Proto.Name),
                    ("votes", result.totalVotes),
                    ("newVotes", result.newVotes)));
            }

            if (winningMaps.Count > 1)
            {
                sb.AppendLine(Loc.GetString("rmc-distress-signal-next-map-tiebreaker"));
                foreach (var map in winningMaps)
                {
                    sb.AppendLine($"    {map.Proto.Name}");
                }
                picked = _random.Pick(winningMaps);
            }
            else
            {
                picked = winningMaps.First();
            }
            sb.AppendLine(Loc.GetString("rmc-distress-signal-next-map-win", ("winner", picked.Proto.Name)));

            _chatManager.DispatchServerAnnouncement(sb.ToString());

            foreach (var (planet, votes) in planets.Zip(args.Votes))
            {
                var id = planet.Proto.ID;
                _carryoverVotes[id] = _useCarryoverVoting ? _carryoverVotes.GetValueOrDefault(id) + votes : 0;
            }

            _carryoverVotes[picked.Proto.ID] = 0;
            SelectedPlanetMap = picked;
        };
        _currentVote.OnCancelled += _ => _currentVote = null;
    }

    /// <summary>
    /// Checks whether a planet selection vote is currently in progress.
    /// </summary>
    /// <returns>True if a vote is running, false otherwise.</returns>
    public bool HasPlanetVoteRunning()
    {
        return _currentVote != null;
    }

    /// <summary>
    /// Cancels the currently active planet selection vote if one exists.
    /// </summary>
    public void CancelPlanetVote()
    {
        _currentVote?.Cancel();
    }
}
