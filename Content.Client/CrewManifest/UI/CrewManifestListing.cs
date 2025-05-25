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
        // server already sorts entries by job weight > name
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
        foreach (var entry in entries.Entries)
        {
            if (!jobDepartments.TryGetValue(entry.JobPrototype, out var cachedDeps))
            {
                // this shouldn't happen. maybe throw instead?
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
    }
}
