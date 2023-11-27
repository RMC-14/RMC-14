using System.Runtime.InteropServices;
using Content.Server._CM14.Marines;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared._CM14.Marines.Squads;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Coordinates;
using Content.Shared.StatusIcon;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using MarineComponent = Content.Shared._CM14.Marines.MarineComponent;
using SquadMemberComponent = Content.Shared._CM14.Marines.Squads.SquadMemberComponent;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;

namespace Content.Server._CM14.Rules;

public sealed class CMRuleSystem : GameRuleSystem<CMRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MarineSystem _marines = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = AllEntityQuery<CMRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            _antagSelection.AttemptStartGameRule(ev, uid, gameRule.MinPlayers, gameRule);
        }
    }

    private void OnPlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            comp.Hive = Spawn(comp.HiveId);
            if (!SpawnXenoMap((uid, comp)))
            {
                Log.Error("Failed to load xeno map");
                continue;
            }

            var totalXenos = ev.PlayerPool.Count / comp.PlayersPerXeno;

            // TODO CM14 preferences
            for (var i = 0; i < totalXenos; i++)
            {
                var player = _random.PickAndTake(ev.PlayerPool);
                GameTicker.PlayerJoinGame(player);

                // TODO CM14 xeno spawn points
                var xenoEnt = Spawn("CMXenoDrone", comp.XenoMap.ToCoordinates());

                _xeno.MakeXeno(xenoEnt);
                _xeno.SetHive(xenoEnt, comp.Hive);

                // TODO CM14 mind name
                var mind = _mind.GetOrCreateMind(player.UserId);
                _mind.TransferTo(mind, xenoEnt);
            }
        }
    }

    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            if (comp.SquadIds.Count == 0)
                continue;

            foreach (var player in ev.Players)
            {
                if (player.AttachedEntity is not { } playerId ||
                    HasComp<XenoComponent>(playerId) ||
                    HasComp<MarineComponent>(playerId) ||
                    HasComp<SquadMemberComponent>(playerId))
                {
                    continue;
                }

                if (comp.NextSquad >= comp.SquadIds.Count)
                {
                    comp.NextSquad = 0;
                }

                var nextSquad = comp.SquadIds[comp.NextSquad++];
                ref var squad = ref CollectionsMarshal.GetValueRefOrAddDefault(comp.Squads, nextSquad, out var exists);
                if (!exists)
                    squad = Spawn(nextSquad);

                SpriteSpecifier? icon = null;
                if (_mind.TryGetMind(playerId, out var mindId, out _) &&
                    _jobs.MindTryGetJob(mindId, out _, out var job) &&
                    _prototypes.TryIndex(job.Icon, out StatusIconPrototype? jobIcon))
                {
                    icon = jobIcon.Icon;
                }

                _marines.MakeMarine(playerId, icon);
                _squad.SetSquad(playerId, squad);
            }
        }
    }

    private bool SpawnXenoMap(Entity<CMRuleComponent> rule)
    {
        var mapId = _mapManager.CreateMap();
        _console.ExecuteCommand($"planet {mapId} Grasslands");
        rule.Comp.XenoMap = _mapManager.GetMapEntityIdOrThrow(mapId);
        return true;
    }
}
