using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared.Coordinates;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class ForsakenXenoSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private static readonly Dictionary<EntProtoId, float> ForsakenCastes = new()
    {
        ["RMCXenoWarriorForsaken"] = 2,
        ["RMCXenoLurkerForsaken"] = 2,
        ["RMCXenoSpitterForsaken"] = 2,
        ["RMCXenoDroneForsaken"] = 5,
        ["RMCXenoRunnerForsaken"] = 5,
    };

    public List<EntityUid> SpawnForsakenXenos(int count)
    {
        var spawned = new List<EntityUid>();
        if (count <= 0)
            return spawned;

        var points = GetSpawnPoints();
        if (points.Count == 0)
            return spawned;

        if (!_hive.TryGetHiveBySlot(HiveSlots.Forsaken, out var hive))
            return spawned;

        for (var i = 0; i < count; i++)
        {
            var caste = _random.Pick(ForsakenCastes);
            var point = _random.Pick(points);
            var xeno = SpawnAtPosition(caste, point.ToCoordinates());

            _xeno.MakeXeno(xeno);
            _hive.SetHive(xeno, hive);
            EnsureComp<LarvaQueuedComponent>(xeno);

            spawned.Add(xeno);
        }

        return spawned;
    }

    private List<EntityUid> GetSpawnPoints()
    {
        var points = new List<EntityUid>();
        var forsakenQuery = EntityQueryEnumerator<XenoForsakenSpawnPointComponent>();
        while (forsakenQuery.MoveNext(out var uid, out _))
        {
            points.Add(uid);
        }

        if (points.Count > 0)
            return points;

        var fallbackQuery = EntityQueryEnumerator<XenoSpawnPointComponent>();
        while (fallbackQuery.MoveNext(out var uid, out _))
        {
            points.Add(uid);
        }

        return points;
    }
}
