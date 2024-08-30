using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._RMC14.Delete;

public sealed class EntityDeleteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override void Initialize()
    {
        Subs.CVar(_config,
            RMCCVars.RMCEntitiesLogDelete,
            v =>
            {
                EntityManager.EntityDeleted -= OnEntityDeleted;

                if (v)
                    EntityManager.EntityDeleted += OnEntityDeleted;
            },
            true);
    }

    private void OnEntityDeleted(Entity<MetaDataComponent> ent)
    {
        Log.Info(Environment.StackTrace);
    }
}
