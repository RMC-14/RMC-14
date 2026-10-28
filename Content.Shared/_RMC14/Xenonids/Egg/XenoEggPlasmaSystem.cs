using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Egg;

public sealed class XenoEggPlasmaSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public static readonly ProtoId<ReagentPrototype> EggPlasma = "RMCEggPlasma";
    private const string EggSolution = "egg";

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEggComponent, HiveChangedEvent>(OnEggHiveChanged);
    }

    private void OnEggHiveChanged(Entity<XenoEggComponent> ent, ref HiveChangedEvent args)
    {
        if (!_solution.TryGetSolution(ent.Owner, EggSolution, out var solutionEnt, out var solution))
            return;

        var quantity = solution.GetTotalPrototypeQuantity(EggPlasma.Id);
        if (quantity <= FixedPoint2.Zero)
            return;

        solution.RemoveReagent(new ReagentQuantity(EggPlasma.Id, quantity), ignoreReagentData: true);
        solution.AddReagent(CreateEggPlasma(args.Hive?.Owner), quantity);
        _solution.UpdateChemicals(solutionEnt.Value, false);
    }

    public ReagentId CreatePlasma(string plasma, EntityUid source)
    {
        if (plasma != EggPlasma.Id)
            return new ReagentId(plasma, null);

        return CreateEggPlasma(_hive.GetHive(source)?.Owner);
    }

    public EntityUid? GetEggPlasmaHive(Solution solution)
    {
        EntityUid? hive = null;
        var largest = FixedPoint2.Zero;
        foreach (var (reagent, quantity) in solution.Contents)
        {
            if (reagent.Prototype != EggPlasma.Id || quantity <= largest)
                continue;

            if (reagent.Data?.Find(d => d is XenoEggPlasmaHiveData) is not XenoEggPlasmaHiveData data)
                continue;

            hive = GetEntity(data.Hive);
            largest = quantity;
        }

        if (hive is { } uid && (TerminatingOrDeleted(uid) || !HasComp<HiveComponent>(uid)))
            return null;

        return hive;
    }

    private ReagentId CreateEggPlasma(EntityUid? hive)
    {
        return new ReagentId(EggPlasma.Id, new List<ReagentData> { new XenoEggPlasmaHiveData { Hive = GetNetEntity(hive) } });
    }
}
