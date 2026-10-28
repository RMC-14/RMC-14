using Content.Server._RMC14.Chemistry;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server._RMC14.Destructible.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCHandleVolatilesBehavior : IThresholdBehavior
{
    [DataField(required: true)]
    public string Solution = default!;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.SolutionContainerSystem.TryGetSolution(owner, Solution, out _, out var solution) ||
            solution.Volume == 0)
        {
            return;
        }

        var fillFraction = solution.FillFraction;
        var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

        system.EntityManager.System<RMCChemVolatilesSystem>().HandleVolatiles(
            owner,
            solution,
            cause,
            structuralExplosiveScale: fillFraction,
            triggerStructuralExplosive: true);

        system.PuddleSystem.TrySpillAt(coordinates, solution, out _);
    }
}
