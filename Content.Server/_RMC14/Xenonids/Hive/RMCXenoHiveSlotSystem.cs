using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.GameTicking;
using Content.Shared._RMC14.GameStates;
using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class RMCXenoHiveSlotSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCPvsSystem _rmcPvs = default!;

    private static readonly EntProtoId[] SlotProtos =
    [
        "CMXenoHiveNormal",
        "CMXenoHiveCorrupted",
        "CMXenoHiveAlpha",
        "CMXenoHiveBravo",
        "CMXenoHiveCharlie",
        "CMXenoHiveDelta",
        "CMXenoHiveRenegade",
        "CMXenoHiveForsaken",
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning, before: [typeof(CMDistressSignalRuleSystem)]);
    }

    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        GenerateSlots();
    }

    public List<EntityUid> GenerateSlots()
    {
        var created = new List<EntityUid>();

        var existing = EntityQueryEnumerator<HiveSlotComponent>();
        if (existing.MoveNext(out _, out _))
            return created;

        foreach (var proto in SlotProtos)
        {
            var slot = Spawn(proto);
            _rmcPvs.AddGlobalOverride(slot);
            created.Add(slot);
        }

        return created;
    }
}
