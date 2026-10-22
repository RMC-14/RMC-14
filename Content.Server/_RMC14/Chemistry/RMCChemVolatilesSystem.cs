using Content.Server._RMC14.Scorch;
using Content.Server._RMC14.Smoke;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Chemistry;

public sealed class RMCChemVolatilesSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedRMCFlamerSystem _flamer = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly RMCSmokeSystem _smoke = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ChemFireProto = "RMCTileFireGenerated";
    private static readonly EntProtoId PhosphorusSmoke = "RMCSmokePhosphorus";
    private static readonly ProtoId<ReagentPrototype> Phosphorus = "RMCPhosphorus";
    private static readonly ProtoId<ExplosionPrototype> ExplosionType = "RMC";

    private const int MinRadius = 1;
    private const int MaxRadius = 5;
    private const int MinIntensity = 3;
    private const int MaxIntensity = 20;
    private const int MinDuration = 3;
    private const int MaxDuration = 24;
    private const float PhosphorusPerSmokeRadius = 10f;

    public const float MaxMixingExplosivePower = 175;

    private const float PowerToIntensity = 40f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExecuteEntityEffectEvent<RMCVolatileReactionEffect>>(OnVolatileReaction);
    }

    private void OnVolatileReaction(ref ExecuteEntityEffectEvent<RMCVolatileReactionEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs { Source: { } solution } reagentArgs)
            return;

        if (!_rmcReagent.TryIndex(args.Effect.Reagent, out var reagent) ||
            reagent.Overdose is not { } overdose ||
            reagentArgs.Quantity * args.Effect.Amount <= overdose ||
            !IsVolatileOrFireSupplement(reagent))
        {
            return;
        }

        var solutionUid = reagentArgs.TargetEntity;
        var owner = TryComp(solutionUid, out ContainedSolutionComponent? contained) ? contained.Container : solutionUid;

        if (HasComp<MobStateComponent>(owner))
            return;

        if (!HandleVolatiles(owner, solution, null, maxExplosivePower: MaxMixingExplosivePower))
            return;

        if (TryComp(solutionUid, out SolutionComponent? solutionComp))
            _solution.RemoveAllSolution((solutionUid, solutionComp));
    }

    private bool IsVolatileOrFireSupplement(ReagentPrototype reagent)
    {
        foreach (var property in _rmcReagent.GetProperties(reagent.ID))
        {
            if (property.IsVolatile || property.IsFireSupplement)
                return true;
        }

        return false;
    }

    /// <returns>True if a fire or an explosion was started.</returns>
    public bool HandleVolatiles(
        EntityUid owner,
        Solution solution,
        EntityUid? cause,
        float explosiveScale = 1f,
        float? maxExplosivePower = null,
        bool triggerStructuralExplosive = false)
    {
        var coordinates = Transform(owner).Coordinates;
        var handled = false;

        var aggregate = _flamer.GetAggregateFireStats(solution);
        if (aggregate.Intensity >= 1)
        {
            var radius = Math.Clamp((int) aggregate.Radius, MinRadius, MaxRadius);
            var intensity = Math.Clamp((int) aggregate.Intensity, MinIntensity, MaxIntensity);
            var duration = Math.Clamp((int) aggregate.Duration, MinDuration, MaxDuration);

            _flammable.SpawnFireDiamond(ChemFireProto, coordinates, radius, intensity, duration, aggregate.FirePenetrating, aggregate.FireColor);
            handled = true;

            var phosphorus = solution.GetTotalPrototypeQuantity(Phosphorus);
            if (phosphorus > FixedPoint2.Zero)
            {
                var smokeRadius = Math.Clamp((int) (phosphorus.Float() / PhosphorusPerSmokeRadius), 1, MaxRadius - 1);
                _smoke.SpawnEvenSmoke(PhosphorusSmoke, coordinates, smokeRadius);
            }
        }

        // (Σ volume*power acros explosive property reagents)
        var explosivePower = 0f;
        var explosiveFalloff = 0f;
        foreach (var quantity in solution.Contents)
        {
            if (!_rmcReagent.TryGetProperty<Explosive>(quantity.Reagent.Prototype, out var explosive))
                continue;

            explosivePower += explosive.PowerDelta * (float) quantity.Quantity;
            explosiveFalloff += explosive.FalloffDelta * (float) quantity.Quantity;
        }
        explosivePower *= explosiveScale;
        explosiveFalloff *= explosiveScale;

        if (maxExplosivePower != null)
            explosivePower = MathF.Min(explosivePower, maxExplosivePower.Value);

        if (triggerStructuralExplosive && TryComp(owner, out ExplosiveComponent? structuralExplosive))
        {
            var totalIntensity = structuralExplosive.TotalIntensity * explosiveScale + explosivePower * PowerToIntensity;
            _explosion.TriggerExplosive(owner, structuralExplosive, false, totalIntensity, user: cause);
            handled = true;
        }
        else if (explosivePower > 0)
        {
            EnsureComp<CMExplosionEffectComponent>(owner);
            EnsureComp<RMCScorchEffectComponent>(owner);

            var totalIntensity = explosivePower * PowerToIntensity;
            var slope = MathF.Max(0.1f, 1f + explosiveFalloff);
            var mapCoordinates = _transform.ToMapCoordinates(coordinates);
            _explosion.QueueExplosion(mapCoordinates, ExplosionType, totalIntensity, slope, MathF.Min(totalIntensity, 30f), cause);

            var ev = new CMExplosiveTriggeredEvent();
            RaiseLocalEvent(owner, ref ev);
            handled = true;
        }

        return handled;
    }
}
