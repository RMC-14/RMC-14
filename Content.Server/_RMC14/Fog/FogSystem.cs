using Content.Server.GameTicking;
using Content.Shared._RMC14.Map;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Fog;

public sealed class FogSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound)
            return;

        var allRemovers = new Dictionary<EntProtoId, List<Entity<RandomAnchoredRemoverComponent>>>();
        var removerQuery = EntityQueryEnumerator<RandomAnchoredRemoverComponent>();
        while (removerQuery.MoveNext(out var uid, out var remover))
        {
            allRemovers.GetOrNew(remover.Group).Add((uid, remover));
        }

        if (allRemovers.Count == 0)
            return;

        var (chosenId, chosen) = _random.Pick(allRemovers);
        foreach (var toRemove in chosen)
        {
            var anchoredEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator(toRemove);
            while (anchoredEnumerator.MoveNext(out var anchored))
            {
                if (anchored == toRemove.Owner)
                    continue;

                if (_entityWhitelist.IsWhitelistPass(toRemove.Comp.Whitelist, anchored))
                    QueueDel(anchored);
            }
        }

        foreach (var removers in allRemovers.Values)
        {
            foreach (var remover in removers)
            {
                QueueDel(remover);
            }
        }

        var spawnerQuery = EntityQueryEnumerator<RandomAnchoredSpawnerComponent>();
        while (spawnerQuery.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Group != chosenId ||
                spawner.Spawn is not { } spawn)
            {
                continue;
            }

            Spawn(spawn, _transform.GetMoverCoordinates(uid));
        }
    }
}
