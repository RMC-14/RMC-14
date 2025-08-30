using Content.Server.Ghost.Roles.Components;
using Content.Server.Jobs;
using Content.Shared.Clothing.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Humanoid;

public sealed class RMCHumanoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCJobSpawnerComponent, ComponentInit>(OnAddJobInit);
    }

    private void OnAddJobInit(Entity<RMCJobSpawnerComponent> ent, ref ComponentInit args)
    {
        if (!_prototype.TryIndex(ent.Comp.Job, out var job))
            return;

        if (TryComp(ent, out GhostRoleComponent? ghostRole))
        {
            ghostRole.RoleName = job.LocalizedName;

            if (job.LocalizedDescription is { } description)
                ghostRole.RoleDescription = description;
        }

        if (ent.Comp.Loadout &&
            job.StartingGear is { } gear)
        {
            var loadout = new LoadoutComponent();
            loadout.StartingGear ??= [];
            loadout.StartingGear.Add(gear);
            AddComp(ent, loadout);
        }

        foreach (var special in job.Special)
        {
            if (special is AddComponentSpecial add)
                EntityManager.AddComponents(ent, add.Components, add.RemoveExisting);
        }
    }
}
