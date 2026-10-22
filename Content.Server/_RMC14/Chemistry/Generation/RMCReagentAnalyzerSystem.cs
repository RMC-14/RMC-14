using System.Linq;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCReagentAnalyzerSystem : SharedRMCReagentAnalyzerSystem
{
    private static readonly ProtoId<ChemGeneratorPropertyPrototype> DnaDisintegratingProperty = "DNADisintegrating";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void DoAnalysis(Entity<RMCReagentAnalyzerComponent> ent)
    {
        var comp = ent.Comp;
        comp.Processing = false;
        Dirty(ent);

        try
        {
            AnalyzeSample(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Error analyzing reagent sample on {ToPrettyString(ent)}: {e}");
            Fail(ent, "The analyzer beeps: SAMPLE EMPTY.");
        }
    }

    private void AnalyzeSample(Entity<RMCReagentAnalyzerComponent> ent)
    {
        var comp = ent.Comp;

        var sample = GetSample(ent);
        if (sample is not { } sampleUid ||
            !_solution.TryGetMixableSolution(sampleUid, out _, out var solution) ||
            solution.Contents.Count == 0)
        {
            Fail(ent, "The analyzer beeps: SAMPLE EMPTY.");
            return;
        }

        if (solution.Contents.Count > 1)
        {
            Fail(ent, "The analyzer beeps: SAMPLE CONTAMINATED. A pure sample is required for analysis.");
            return;
        }

        if (solution.Volume < comp.RequiredVolume)
        {
            Fail(ent, $"The analyzer beeps: SAMPLE SIZE INSUFFICIENT. A sample size of {comp.RequiredVolume} units is required for analysis.");
            return;
        }

        var reagentId = solution.Contents[0].Reagent.Prototype;
        if (!_rmcReagent.TryIndex(reagentId, out var reagent))
        {
            Fail(ent, "The analyzer beeps: SAMPLE EMPTY.");
            return;
        }

        _metaData.SetEntityName(sampleUid, $"vial ({reagent.LocalizedName})");

        comp.VisualState = RMCReagentAnalyzerVisualState.Finished;
        Dirty(ent);
        UpdateAppearance(ent);

        _generator.PrintReport(Transform(ent).Coordinates, reagentId, reagent, comp.SampleNumber++, "XRF Scans");
        _audio.PlayPvs(comp.FinishSound, ent);

        if (reagent.ChemClass >= ChemClass.Special && !_research.IsIdentified(reagentId))
        {
            if (_generator.GetProperties(reagent).Any(p => p.Property.ID == DnaDisintegratingProperty.Id) &&
                !_research.TryDiscoverDnaDisintegrating(reagent.LocalizedName))
            {
                return;
            }

            var points = _generator.IsGenerated(reagentId) ? FixedPoint2.Zero : reagent.ObjectiveValue;
            _research.TryAwardIdentificationCredits(reagentId, reagent.CreditReward, points);
        }
    }

    private void Fail(Entity<RMCReagentAnalyzerComponent> ent, string reason)
    {
        var comp = ent.Comp;
        comp.VisualState = RMCReagentAnalyzerVisualState.Failed;
        Dirty(ent);
        UpdateAppearance(ent);

        _generator.PrintErrorReport(Transform(ent).Coordinates, reason, comp.SampleNumber++);
        _audio.PlayPvs(comp.FailSound, ent);
    }
}
