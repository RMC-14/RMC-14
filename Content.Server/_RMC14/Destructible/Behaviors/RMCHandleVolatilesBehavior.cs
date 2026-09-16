using Content.Server._RMC14.Scorch;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Explosion.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Destructible.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCHandleVolatilesBehavior : IThresholdBehavior
{
    [DataField(required: true)]
    public string Solution = default!;

    [DataField]
    public EntProtoId FireProto = "RMCTileFire";

    private const int MinRadius = 1;
    private const int MaxRadius = 5;
    private const int MinIntensity = 3;
    private const int MaxIntensity = 20;
    private const int MinDuration = 3;
    private const int MaxDuration = 24;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.SolutionContainerSystem.TryGetSolution(owner, Solution, out _, out var solution) ||
            solution.Volume == 0)
        {
            return;
        }

        var fillFraction = solution.FillFraction;
        var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

        var flamer = system.EntityManager.System<SharedRMCFlamerSystem>();
        var aggregate = flamer.GetAggregateFireStats(solution);
        if (aggregate.Intensity > 0 || aggregate.Duration > 0 || aggregate.Radius > 0)
        {
            var flammable = system.EntityManager.System<SharedRMCFlammableSystem>();
            var radius = Math.Clamp((int) aggregate.Radius, MinRadius, MaxRadius);
            var intensity = Math.Clamp((int) aggregate.Intensity, MinIntensity, MaxIntensity);
            var duration = Math.Clamp((int) aggregate.Duration, MinDuration, MaxDuration);

            // Use whichever reagent is actually in the tanks fire
            var fireProto = FireProto;
            Color? fireColor = null;
            if (solution.TryFirstOrNull(out var firstReagent))
            {
                var reagentSystem = system.EntityManager.System<RMCReagentSystem>();
                if (reagentSystem.TryIndex(firstReagent.Value.Reagent.Prototype, out var reagent))
                {
                    fireProto = reagent.FireEntity;
                    fireColor = flammable.GetFireColor(reagent);
                }
            }

            flammable.SpawnFireDiamond(fireProto, coordinates, radius, intensity, duration, aggregate.FirePenetrating, fireColor);
        }

        system.PuddleSystem.TrySpillAt(coordinates, solution, out _);

        // (Σ volume*power acros explosive property reagents)
        var explosiveReagentSystem = system.EntityManager.System<RMCReagentSystem>();
        var explosivePower = 0f;
        var explosiveFalloff = 0f;
        foreach (var quantity in solution.Contents)
        {
            if (!explosiveReagentSystem.TryIndex(quantity.Reagent.Prototype, out var quantityReagent) || quantityReagent.Metabolisms == null)
                continue;

            foreach (var (_, entry) in quantityReagent.Metabolisms)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is Explosive explosive)
                    {
                        explosivePower += explosive.PowerDelta * (float) quantity.Quantity;
                        explosiveFalloff += explosive.FalloffDelta * (float) quantity.Quantity;
                    }
                }
            }
        }
        explosivePower *= fillFraction;
        explosiveFalloff *= fillFraction;

        const float powerToIntensity = 40f;

        var hasStructural = system.EntityManager.TryGetComponent(owner, out ExplosiveComponent? structuralExplosive);
        if (hasStructural)
        {
            var totalIntensity = structuralExplosive!.TotalIntensity * fillFraction + explosivePower * powerToIntensity;
            system.ExplosionSystem.TriggerExplosive(owner, structuralExplosive, false, totalIntensity, user: cause);
        }
        else if (explosivePower > 0)
        {
            system.EntityManager.EnsureComponent<CMExplosionEffectComponent>(owner);
            system.EntityManager.EnsureComponent<RMCScorchEffectComponent>(owner);

            var totalIntensity = explosivePower * powerToIntensity;
            var slope = MathF.Max(0.1f, 1f + explosiveFalloff);
            var mapCoordinates = system.EntityManager.System<SharedTransformSystem>().ToMapCoordinates(coordinates);
            system.ExplosionSystem.QueueExplosion(mapCoordinates, "RMC", totalIntensity, slope, MathF.Min(totalIntensity, 30f), cause);

            var ev = new CMExplosiveTriggeredEvent();
            system.EntityManager.EventBus.RaiseLocalEvent(owner, ref ev);
        }
    }
}
