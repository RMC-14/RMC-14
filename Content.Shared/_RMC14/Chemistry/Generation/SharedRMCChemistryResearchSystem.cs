using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCChemistryResearchSystem : EntitySystem
{
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
