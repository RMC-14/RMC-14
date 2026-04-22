using System.Linq;
using Content.Shared._RMC14.Rules;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public static class SurvivorVariantJobPreferences
{
    public static readonly ProtoId<DepartmentPrototype> SurvivorDepartment = "CMSurvivor";

    public sealed class MapVariantJobs
    {
        public MapVariantJobs(string mapName, int order)
        {
            MapName = mapName;
            Order = order;
        }

        public string MapName { get; }
        public int Order { get; }
        public float RegularChance { get; set; }
        public List<JobPrototype> RegularJobs { get; } = new();
        public List<InsertVariantJobs> Inserts { get; } = new();
        public IEnumerable<JobPrototype> AllJobs => RegularJobs.Concat(Inserts.SelectMany(insert => insert.Jobs));
    }

    public sealed class InsertVariantJobs
    {
        public InsertVariantJobs(string scenarioName, string displayName, int order)
        {
            ScenarioName = scenarioName;
            DisplayName = displayName;
            Order = order;
        }

        public string ScenarioName { get; }
        public string DisplayName { get; }
        public int Order { get; }
        public float Chance { get; set; }
        public List<JobPrototype> Jobs { get; } = new();
    }

    public static Dictionary<ProtoId<JobPrototype>, List<MapVariantJobs>> GetVariantJobsByBase(
        IPrototypeManager prototypes,
        IComponentFactory compFactory)
    {
        var baseJobs = new HashSet<ProtoId<JobPrototype>>();
        if (prototypes.TryIndex(SurvivorDepartment, out DepartmentPrototype? survivorDepartment))
            baseJobs.UnionWith(survivorDepartment.Roles);

        var result = new Dictionary<ProtoId<JobPrototype>, List<MapVariantJobs>>();
        var mapOrder = 0;
        foreach (var entity in prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!entity.TryGetComponent(out RMCPlanetMapPrototypeComponent? planet, compFactory))
                continue;

            var currentMapOrder = mapOrder++;
            var grouped = new Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>>();
            AddVariantJobs(grouped, planet.SurvivorJobVariants);
            AddOverrideJobs(grouped, planet.SurvivorJobOverrides);
            AddMapVariantJobs(
                result,
                baseJobs,
                grouped,
                entity.Name,
                currentMapOrder,
                null,
                null,
                0,
                GetRegularScenarioChance(planet),
                prototypes);

            var fallbackScenarioOrder = 0;
            if (planet.SurvivorJobVariantScenarios != null)
            {
                foreach (var (scenario, scenarioVariants) in planet.SurvivorJobVariantScenarios)
                {
                    grouped = new Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>>();
                    AddVariantJobs(grouped, scenarioVariants);
                    AddMapVariantJobs(
                        result,
                        baseJobs,
                        grouped,
                        entity.Name,
                        currentMapOrder,
                        scenario,
                        GetScenarioDisplayName(scenario),
                        GetScenarioOrder(planet, scenario, fallbackScenarioOrder++),
                        GetScenarioChance(planet, scenario),
                        prototypes);
                }
            }

            if (planet.SurvivorJobOverrideScenarios != null)
            {
                foreach (var (scenario, scenarioOverrides) in planet.SurvivorJobOverrideScenarios)
                {
                    grouped = new Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>>();
                    AddOverrideJobs(grouped, scenarioOverrides);
                    AddMapVariantJobs(
                        result,
                        baseJobs,
                        grouped,
                        entity.Name,
                        currentMapOrder,
                        scenario,
                        GetScenarioDisplayName(scenario),
                        GetScenarioOrder(planet, scenario, fallbackScenarioOrder++),
                        GetScenarioChance(planet, scenario),
                        prototypes);
                }
            }
        }

        foreach (var mapGroups in result.Values)
        {
            mapGroups.Sort(CompareMapVariantJobs);
            foreach (var mapGroup in mapGroups)
                mapGroup.Inserts.Sort(CompareInsertVariantJobs);
        }

        return result;
    }

    private static int CompareMapVariantJobs(MapVariantJobs a, MapVariantJobs b)
    {
        var order = a.Order.CompareTo(b.Order);
        if (order != 0)
            return order;

        return string.Compare(a.MapName, b.MapName, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareInsertVariantJobs(InsertVariantJobs a, InsertVariantJobs b)
    {
        var order = a.Order.CompareTo(b.Order);
        if (order != 0)
            return order;

        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddMapVariantJobs(
        Dictionary<ProtoId<JobPrototype>, List<MapVariantJobs>> result,
        HashSet<ProtoId<JobPrototype>> baseJobs,
        Dictionary<ProtoId<JobPrototype>, HashSet<ProtoId<JobPrototype>>> grouped,
        string mapName,
        int mapOrder,
        string? scenarioName,
        string? scenarioDisplayName,
        int scenarioOrder,
        float chance,
        IPrototypeManager prototypes)
    {
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
            var mapGroups = result.GetOrNew(baseJob);
            var mapGroup = mapGroups.FirstOrDefault(group => group.MapName == mapName);
            if (mapGroup == null)
            {
                mapGroup = new MapVariantJobs(mapName, mapOrder);
                mapGroups.Add(mapGroup);
            }

            if (scenarioName == null)
            {
                mapGroup.RegularChance = Math.Max(mapGroup.RegularChance, chance);
                AddJobs(mapGroup.RegularJobs, jobs);
                continue;
            }

            var insert = mapGroup.Inserts.FirstOrDefault(group =>
                group.ScenarioName.Equals(scenarioName, StringComparison.OrdinalIgnoreCase));
            if (insert == null)
            {
                insert = new InsertVariantJobs(scenarioName, scenarioDisplayName ?? GetScenarioDisplayName(scenarioName), scenarioOrder);
                mapGroup.Inserts.Add(insert);
            }

            insert.Chance = Math.Max(insert.Chance, chance);
            AddJobs(insert.Jobs, jobs);
        }
    }

    private static void AddJobs(List<JobPrototype> existing, List<JobPrototype> jobs)
    {
        var existingIds = existing.Select(job => job.ID).ToHashSet();
        foreach (var job in jobs)
        {
            if (existingIds.Add(job.ID))
                existing.Add(job);
        }

        existing.Sort(JobUIComparer.Instance);
    }

    private static int GetScenarioOrder(RMCPlanetMapPrototypeComponent planet, string scenarioName, int fallback)
    {
        if (planet.NightmareScenarios == null)
            return fallback;

        var order = 0;
        foreach (var scenario in planet.NightmareScenarios)
        {
            if (scenario.ScenarioName.Equals(scenarioName, StringComparison.OrdinalIgnoreCase))
                return order;

            order++;
        }

        return order + fallback;
    }

    private static string GetScenarioDisplayName(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            return scenarioName;

        var builder = new System.Text.StringBuilder(scenarioName.Length);
        var capitalize = true;
        foreach (var c in scenarioName)
        {
            if (c == '_' || char.IsWhiteSpace(c))
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    builder.Append(' ');

                capitalize = true;
                continue;
            }

            builder.Append(capitalize ? char.ToUpperInvariant(c) : c);
            capitalize = false;
        }

        return builder.ToString();
    }

    private static float GetRegularScenarioChance(RMCPlanetMapPrototypeComponent planet)
    {
        if (planet.NightmareScenarios == null || planet.NightmareScenarios.Count == 0)
            return 1f;

        var totalChance = 0f;
        var regularChance = 0f;
        var survivorScenarioNames = GetSurvivorScenarioNames(planet);
        foreach (var scenario in planet.NightmareScenarios)
        {
            totalChance += scenario.ScenarioProbability;
            if (IsRegularScenario(scenario.ScenarioName) || !ContainsScenario(survivorScenarioNames, scenario.ScenarioName))
                regularChance += scenario.ScenarioProbability;
        }

        if (totalChance < 1f)
            regularChance += 1f - totalChance;

        return Math.Clamp(regularChance, 0f, 1f);
    }

    private static float GetScenarioChance(RMCPlanetMapPrototypeComponent planet, string scenarioName)
    {
        if (planet.NightmareScenarios == null || planet.NightmareScenarios.Count == 0)
            return IsRegularScenario(scenarioName) ? 1f : 0f;

        var chance = 0f;
        foreach (var scenario in planet.NightmareScenarios)
        {
            if (scenario.ScenarioName.Equals(scenarioName, StringComparison.OrdinalIgnoreCase))
                chance += scenario.ScenarioProbability;
        }

        return Math.Clamp(chance, 0f, 1f);
    }

    private static bool IsRegularScenario(string scenarioName)
    {
        return string.IsNullOrWhiteSpace(scenarioName) ||
               scenarioName.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetSurvivorScenarioNames(RMCPlanetMapPrototypeComponent planet)
    {
        var names = new HashSet<string>();

        if (planet.SurvivorJobScenarios != null)
            names.UnionWith(planet.SurvivorJobScenarios.Keys);

        if (planet.SurvivorJobVariantScenarios != null)
            names.UnionWith(planet.SurvivorJobVariantScenarios.Keys);

        if (planet.SurvivorJobOverrideScenarios != null)
            names.UnionWith(planet.SurvivorJobOverrideScenarios.Keys);

        names.RemoveWhere(IsRegularScenario);
        return names;
    }

    private static bool ContainsScenario(HashSet<string> scenarioNames, string scenarioName)
    {
        foreach (var name in scenarioNames)
        {
            if (name.Equals(scenarioName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
