using System.Runtime.InteropServices;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Shared.CM14.Marines;
using Content.Shared.CM14.Marines.Squads;
using Content.Shared.Coordinates;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Random;
using XenoComponent = Content.Shared.CM14.Xenos.XenoComponent;

namespace Content.Server.CM14.Rules;

public sealed class CMRuleSystem : GameRuleSystem<CMRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SquadSystem _squad = default!;

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
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
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
                var xeno = Spawn("MobXeno", comp.XenoMap.ToCoordinates());

                // TODO CM14 mind name
                var mind = _mind.GetOrCreateMind(player.UserId);
                _roles.MindAddRole(mind, new XenoComponent(), mind);
                _mind.TransferTo(mind, xeno);
                break;
            }
        }
    }

    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SquadIds.Count == 0)
                continue;

            foreach (var player in ev.Players)
            {
                if (player.AttachedEntity is not { } entity ||
                    HasComp<XenoComponent>(entity) ||
                    HasComp<MarineComponent>(entity) ||
                    HasComp<SquadMemberComponent>(entity))
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

                AddComp<MarineComponent>(entity);
                _squad.SetSquad(entity, squad);
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
