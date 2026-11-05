using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCChemistryResearchSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;

    public virtual void EnsureNotesGenerated(Entity<RMCChemSynthesisNotesComponent> notes)
    {
    }

    public void SetReportFromNotes(EntityUid report, RMCChemSynthesisNotesComponent notes)
    {
        if (notes.ReagentId is not { } reagentId)
            return;

        var comp = EnsureComp<RMCChemResearchReportComponent>(report);
        comp.ReagentId = reagentId;
        comp.Properties = notes.Properties.Select(p => new ChemReportProperty { PropertyId = p.PropertyId, Level = p.Level }).ToList();
        comp.Overdose = notes.Overdose;
        comp.CriticalOverdose = notes.CriticalOverdose;
        comp.Completed = notes.FullReport;
        comp.Valid = true;
        comp.IsGenerated = _rmcReagent.IsGenerated(reagentId);
        Dirty(report, comp);
    }

    public bool TryGetResearch([NotNullWhen(true)] out Entity<RMCChemistryResearchComponent>? research)
    {
        var query = EntityQueryEnumerator<RMCChemistryResearchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            research = (uid, comp);
            return true;
        }

        research = default;
        return false;
    }

    public bool HasSufficientClearance(int tier)
    {
        if (!TryGetResearch(out var research))
            return false;

        if (tier >= research.Value.Comp.XAccessClearanceTier)
            return research.Value.Comp.ReachedXAccess;

        return research.Value.Comp.ClearanceLevel >= tier;
    }

    public int GetClearanceUpgradeCost(RMCChemistryResearchComponent research)
    {
        return Math.Max(research.ResearchLevelIncreaseMultiplier * research.ClearanceLevel + 1, 1);
    }
}
