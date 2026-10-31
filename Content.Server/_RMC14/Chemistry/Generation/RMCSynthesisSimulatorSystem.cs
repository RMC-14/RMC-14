using System.Linq;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCSynthesisSimulatorSystem : SharedRMCSynthesisSimulatorSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;

    protected override void DoSimulation(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        var comp = ent.Comp;
        comp.Simulating = false;

        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport) || targetReport.ReagentId is not { } targetId)
        {
            Dirty(ent);
            UpdateAppearance(ent);
            return;
        }

        var properties = GetReportProperties(targetReport);

        ChemGeneratorPropertyPrototype? referenceProperty = null;
        var referenceLevel = 0;
        if (comp.Mode is SynthesisMode.Relate or SynthesisMode.Add &&
            TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport))
        {
            TryGetProperty(referenceReport, comp.ReferenceProperty, out referenceProperty!, out referenceLevel);
        }

        switch (comp.Mode)
        {
            case SynthesisMode.Amplify:
                if (TryGetProperty(targetReport, comp.TargetProperty, out var amplifyProperty, out var amplifyLevel))
                {
                    properties.RemoveAll(p => p.Property.ID == amplifyProperty.ID);
                    properties.Add((amplifyProperty, amplifyLevel + 1));
                }
                break;

            case SynthesisMode.Suppress:
                if (TryGetProperty(targetReport, comp.TargetProperty, out var suppressProperty, out var suppressLevel))
                {
                    properties.RemoveAll(p => p.Property.ID == suppressProperty.ID);
                    properties.Add((suppressProperty, Math.Max(suppressLevel - 1, 0)));
                }
                break;

            case SynthesisMode.Relate:
                if (referenceProperty != null && TryGetProperty(targetReport, comp.TargetProperty, out var relateTarget, out _))
                {
                    properties.RemoveAll(p => p.Property.ID == relateTarget.ID);
                    _generator.InsertProperty(properties, referenceProperty, referenceLevel);
                }
                break;

            case SynthesisMode.Add:
                if (referenceProperty != null)
                    _generator.InsertProperty(properties, referenceProperty, referenceLevel);
                break;
        }

        var overdose = GetPredictedOverdose(ent)!.Value;

        var signature = _generator.ComputeSimulationSignature(_generator.GetRootId(targetId.Id), properties);
        if (_generator.TryGetSimulationResult(signature, out var existingId) && _rmcReagent.TryIndex(existingId, out var existingReagent))
        {
            comp.RecipeCandidates = null;
            comp.PendingProperties = null;
            comp.PendingNewIngredientIds = null;
            comp.PendingSignature = null;
            Dirty(ent);
            UpdateAppearance(ent);
            _generator.PrintReport(Transform(ent).Coordinates, existingId, existingReagent, simulatorReport: true);
            StartPrintingAnimation(ent);
            _audio.PlayPvs(comp.FinishSound, ent);
            return;
        }

        var tier = _generator.TryGetGeneratedTier(targetId, out var t) ? t : 1;

        var hasExistingRecipe = _generator.TryGetRecipeReactants(targetId.Id, out var existingReactants);
        var candidateCount = hasExistingRecipe ? comp.RecipeCandidateCount : 1;

        var candidates = new List<List<(string Id, int Amount, bool Catalyst)>>();
        var newIngredientIds = new List<string?>();

        for (var i = 0; i < candidateCount; i++)
        {
            // retries to avoid rolling a duplicate rcipe
            for (var attempt = 0; attempt < comp.RecipeRollAttempts; attempt++)
            {
                if (!_generator.TryRollRecipeCandidate(targetId, tier, out var ingredients, out var newIngredientId))
                    continue;

                if (hasExistingRecipe && _generator.IsDuplicateRecipe(ingredients, existingReactants))
                    continue;

                if (candidates.Any(c => _generator.IsDuplicateRecipe(ingredients, c)))
                    continue;

                candidates.Add(ingredients);
                newIngredientIds.Add(newIngredientId);
                break;
            }
        }

        if (candidates.Count == 0)
        {
            comp.SimulationFailed = true;
            comp.RecipeCandidates = null;
            comp.PendingProperties = null;
            comp.PendingNewIngredientIds = null;
            comp.PendingSignature = null;
            Dirty(ent);
            UpdateAppearance(ent);
            _audio.PlayPvs(comp.FailSound, ent);
            return;
        }

        comp.PendingProperties = properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList();
        comp.PendingOverdose = overdose;
        comp.PendingTier = tier;
        comp.RecipeCandidates = candidates
            .Select(c => c.Select(x => new RecipeCandidateIngredient { Id = x.Id, Amount = x.Amount, Catalyst = x.Catalyst }).ToList())
            .ToList();
        comp.PendingNewIngredientIds = newIngredientIds;
        comp.PendingSignature = signature;
        Dirty(ent);

        if (!hasExistingRecipe)
        {
            FinalizeSimulation(ent, 0);
            return;
        }

        comp.Picking = true;
        UpdateAppearance(ent);

        _audio.PlayPvs(comp.FinishSound, ent);
    }

    protected override void FinalizeSimulation(Entity<RMCSynthesisSimulatorComponent> ent, int recipeIndex)
    {
        var comp = ent.Comp;
        comp.Picking = false;

        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport) ||
            targetReport.ReagentId is not { } targetId ||
            comp.PendingProperties is not { } pendingProperties ||
            comp.RecipeCandidates is not { } candidates ||
            recipeIndex < 0 ||
            recipeIndex >= candidates.Count)
        {
            comp.RecipeCandidates = null;
            comp.PendingProperties = null;
            comp.PendingNewIngredientIds = null;
            comp.PendingSignature = null;
            Dirty(ent);
            UpdateAppearance(ent);
            return;
        }

        if (!_research.TrySpendCredits(comp.PendingCost))
        {
            comp.RecipeCandidates = null;
            comp.PendingProperties = null;
            comp.PendingNewIngredientIds = null;
            comp.PendingSignature = null;
            Dirty(ent);
            UpdateAppearance(ent);
            _audio.PlayPvs(comp.FailSound, ent);
            _popup.PopupEntity("Insufficient research credits.", ent, PopupType.MediumCaution);
            return;
        }

        var properties = new List<(ChemGeneratorPropertyPrototype Property, int Level)>();
        foreach (var p in pendingProperties)
        {
            if (ProtoManager.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
                properties.Add((proto, p.Level));
        }

        var ingredients = candidates[recipeIndex].Select(c => (c.Id, c.Amount, c.Catalyst)).ToList();

        var baseReagent = _rmcReagent.Index(targetId);
        var variantId = _generator.GenerateVariant(baseReagent, properties, comp.PendingTier, ingredients, comp.PendingOverdose);

        if (comp.PendingSignature is { } signature)
            _generator.RegisterSimulationResult(signature, variantId);

        if (comp.PendingNewIngredientIds is { } newIngredientIds &&
            recipeIndex < newIngredientIds.Count &&
            newIngredientIds[recipeIndex] is { } newIngredientId &&
            _generator.TryGetChemClass(newIngredientId, out var newIngredientClass) &&
            newIngredientClass >= ChemClass.Rare)
        {
            _research.AddCredits(1);
        }

        if (comp.Mode == SynthesisMode.Add &&
            TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport) &&
            referenceReport.ReagentId is { } referenceId)
        {
            _research.LockLineage(referenceId.Id);
        }

        var variantReagent = _rmcReagent.Index(variantId);
        _generator.PrintReport(Transform(ent).Coordinates, variantId, variantReagent, category: "Synthesis Simulations", simulatorReport: true);

        comp.RecipeCandidates = null;
        comp.PendingProperties = null;
        comp.PendingNewIngredientIds = null;
        comp.PendingSignature = null;
        StartPrintingAnimation(ent);

        _audio.PlayPvs(comp.FinishSound, ent);
    }
}
