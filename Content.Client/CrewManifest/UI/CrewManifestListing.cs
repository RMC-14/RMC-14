using System.Linq;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestListing : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        RMCAddCrewManifestEntries(entries);
        return;
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        foreach (var entry in entries.Entries)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        foreach (var item in entryList)
        {
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries));
        }
    }

    public void RMCAddCrewManifestEntries(CrewManifestEntries entries)
    {
        // server already sorts entries by job display weight > name
        // if we keep the order we don't need to do it ourselves!
        // we still need to group entries by department and split marines into squads though

        // create an inverse role -> department lookup
        var jobDepartments = new Dictionary<ProtoId<JobPrototype>, List<DepartmentPrototype>>();
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            foreach (var roleId in department.Roles.Where(roleId => _prototypeManager.HasIndex(roleId)))
            {
                jobDepartments.GetOrNew(roleId).Add(department);
            }
        }

        // still sorted non-marine entries grouped by department
        var departmentDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();
        // still sorted marine entries grouped by squad
        var squadDict = new Dictionary<string, List<CrewManifestEntry>>();
        foreach (var entry in entries.Entries)
        {
            if (!jobDepartments.TryGetValue(entry.JobPrototype, out var cachedDeps))
            {
                // this shouldn't happen. maybe throw instead?
                continue;
            }

            // squad role?
            if (!string.IsNullOrEmpty(entry.Squad))
            {
                squadDict.GetOrNew(entry.Squad).Add(entry);
                continue;
            }

            foreach (var department in cachedDeps)
            {
                departmentDict.GetOrNew(department).Add(entry);
            }
        }

        // sort by department weight
        foreach (var (department, depEntries) in departmentDict
                     .OrderBy(kvp => kvp.Key, DepartmentUIComparer.Instance))
        {
            AddChild(new CrewManifestSection(
                _prototypeManager,
                _spriteSystem,
                Loc.GetString(department.Name),
                depEntries));
        }

        // sort by squad name
        foreach (var (squad, squadEntries) in squadDict
                     .OrderBy(kvp => kvp.Key))
        {
            // server already sorts by job weight > name
            AddChild(new CrewManifestSection(
                _prototypeManager,
                _spriteSystem,
                Loc.GetString(squad),
                squadEntries));
        }
    }
}
