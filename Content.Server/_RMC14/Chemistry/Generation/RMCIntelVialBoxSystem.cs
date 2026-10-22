using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Intel.Detector;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCIntelVialBoxSystem : EntitySystem
{
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    private static readonly EntProtoId VialProto = "RMCVial";

    private const int SlotCount = 6;
    private const string VialSolution = "beaker";
    private static readonly FixedPoint2 VialFillAmount = FixedPoint2.New(30);

    public override void Initialize()
    {
        SubscribeLocalEvent<IntelVialBoxComponent, MapInitEvent>(OnVialBoxMapInit);
        SubscribeLocalEvent<IntelVialBoxComponent, EntInsertedIntoContainerMessage>(OnVialBoxInserted);
        SubscribeLocalEvent<IntelVialBoxComponent, EntRemovedFromContainerMessage>(OnVialBoxRemoved);
        SubscribeLocalEvent<IntelVialComponent, SolutionContainerChangedEvent>(OnVialSolutionChanged);
    }

    private void OnVialBoxMapInit(Entity<IntelVialBoxComponent> box, ref MapInitEvent args)
    {
        if (!TryComp(box, out StorageComponent? storage))
            return;

        var coords = Transform(box).Coordinates;

        // slots are candidates, each with separate 40% chance of holding a real reagent  rest are always empty
        var spawns = _random.Next(1, 5);

        for (var i = 0; i < SlotCount; i++)
        {
            var vial = Spawn(VialProto, coords);
            _storage.Insert(box.Owner, vial, out _, storageComp: storage, playSound: false);

            if (i >= spawns)
                continue;

            if (!_random.Prob(0.4f))
                continue;

            var reagentId = _generator.RollRandomKnownReagent();
            if (string.IsNullOrEmpty(reagentId))
                continue;

            if (!_solutionContainer.TryGetSolution(vial, VialSolution, out var solution, out _) ||
                !_solutionContainer.TryAddReagent(solution.Value, reagentId, VialFillAmount))
            {
                continue;
            }

            EnsureComp<IntelVialComponent>(vial);
            EnsureComp<IntelDetectorTrackedComponent>(vial);
        }
    }

    private void OnVialBoxInserted(Entity<IntelVialBoxComponent> box, ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<IntelVialComponent>(args.Entity))
            EnsureComp<IntelDetectorTrackedComponent>(box);
    }

    private void OnVialBoxRemoved(Entity<IntelVialBoxComponent> box, ref EntRemovedFromContainerMessage args)
    {
        if (TerminatingOrDeleted(box))
            return;

        foreach (var contained in args.Container.ContainedEntities)
        {
            if (!HasComp<IntelVialComponent>(contained))
                continue;

            EnsureComp<IntelDetectorTrackedComponent>(box);
            return;
        }

        RemComp<IntelDetectorTrackedComponent>(box);
    }

    private void OnVialSolutionChanged(Entity<IntelVialComponent> vial, ref SolutionContainerChangedEvent args)
    {
        if (args.Solution.Volume > FixedPoint2.Zero)
            return;

        RemComp<IntelDetectorTrackedComponent>(vial);
        RemCompDeferred<IntelVialComponent>(vial);
    }
}
