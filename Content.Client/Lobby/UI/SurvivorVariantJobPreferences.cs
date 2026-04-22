using Content.Shared._RMC14.Rules;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public static class SurvivorVariantJobPreferences
{
    public static readonly ProtoId<DepartmentPrototype> SurvivorDepartment = "CMSurvivor";

    public static Dictionary<ProtoId<JobPrototype>, List<JobPrototype>> GetVariantJobsByBase(
        IPrototypeManager prototypes,
        IComponentFactory compFactory)
    {
        var baseJobs = new HashSet<ProtoId<JobPrototype>>();
        if (prototypes.TryIndex(SurvivorDepartment, out DepartmentPrototype? survivorDepartment))
            baseJobs.UnionWith(survivorDepartment.Roles);

        var grouped = new Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>>();
        foreach (var entity in prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!entity.TryGetComponent(out RMCPlanetMapPrototypeComponent? planet, compFactory))
                continue;

            AddVariantJobs(grouped, planet.SurvivorJobVariants);
            AddOverrideJobs(grouped, planet.SurvivorJobOverrides);

            if (planet.SurvivorJobVariantScenarios != null)
            {
                foreach (var scenarioVariants in planet.SurvivorJobVariantScenarios.Values)
                    AddVariantJobs(grouped, scenarioVariants);
            }

            if (planet.SurvivorJobOverrideScenarios != null)
            {
                foreach (var scenarioOverrides in planet.SurvivorJobOverrideScenarios.Values)
                    AddOverrideJobs(grouped, scenarioOverrides);
            }
        }

        var result = new Dictionary<ProtoId<JobPrototype>, List<JobPrototype>>();
        foreach (var (baseJob, variantIds) in grouped)
        {
            if (!baseJobs.Contains(baseJob))
                continue;

            variantIds.Remove(baseJob);
            variantIds.ExceptWith(baseJobs);

            var jobs = new List<JobPrototype>();
            foreach (var variantId in variantIds)
            {
                if (!prototypes.TryIndex(variantId, out JobPrototype? job))
                    continue;

                if (!job.IsCM || !job.SetPreference)
                    continue;

                jobs.Add(job);
            }

            if (jobs.Count == 0)
                continue;

            jobs.Sort(JobUIComparer.Instance);
            result[baseJob] = jobs;
        }

        return result;
    }

    private static void AddVariantJobs(
        Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>> grouped,
        Dictionary<ProtoId<JobPrototype>, List<(ProtoId<JobPrototype> Variant, int Amount)>>? variants)
    {
        if (variants == null)
            return;

        foreach (var (baseJob, variantJobs) in variants)
        {
            var jobIds = grouped.GetOrNew(baseJob);
            foreach (var (variant, _) in variantJobs)
                jobIds.Add(variant);
        }
    }

    private static void AddOverrideJobs(
        Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>> grouped,
        Dictionary<ProtoId<JobPrototype>, ProtoId<JobPrototype>>? overrides)
    {
        if (overrides == null)
            return;

        foreach (var (baseJob, overrideJob) in overrides)
            grouped.GetOrNew(baseJob).Add(overrideJob);
    }
}
