using Content.Shared._RMC14.Deploy;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Deploy;

public sealed class RMCDeployServerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCDeployedEntityComponent, ComponentShutdown>(OnDeployedEntityShutdown);
        SubscribeLocalEvent<RMCDeployableComponent, ComponentShutdown>(OnDeployableShutdown);
    }

    /// <summary>
    /// Called when a deployed child entity is being deleted or its component is removed.
    /// Handles cleanup and possible deletion of related entities.
    /// </summary>
    private void OnDeployedEntityShutdown(EntityUid uid, RMCDeployedEntityComponent comp, ComponentShutdown args)
    {
        Logger.Error($"[ServerDeploy] Shutdown called for {uid}, InShutdown={comp.InShutdown}");
        // If already in shutdown, skip further processing
        if (comp.InShutdown)
        {
            Logger.Error($"[ServerDeploy] Already in shutdown for {uid}");
            return;
        }
        comp.InShutdown = true;
        Dirty(uid, comp);

        Logger.Error($"[ServerDeploy] Checking original entity {comp.OriginalEntity}");
        // Try to get the original entity
        if (!EntityManager.EntityExists(comp.OriginalEntity))
        {
            Logger.Error($"[ServerDeploy] Original entity {comp.OriginalEntity} does not exist");
            return;
        }

        if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? origComp))
        {
            Logger.Error($"[ServerDeploy] Original entity {comp.OriginalEntity} has no RMCDeployableComponent");
            return;
        }

        Logger.Error($"[ServerDeploy] Found RMCDeployableComponent on {comp.OriginalEntity}");
        if (origComp is not null)
        {
            // Check if this was a ReactiveParentalSetup
            var setup = origComp.DeploySetups[comp.SetupIndex];
            Logger.Error($"[ServerDeploy] SetupIndex={comp.SetupIndex} Mode={setup.Mode}");
            if (setup.Mode == RMCDeploySetupMode.ReactiveParental)
            {
                // First, collect all entities to delete, then delete them outside the enumeration to avoid reentrancy and collection modification issues.
                var toDelete = new List<EntityUid>();
                var enumerator = EntityManager.EntityQueryEnumerator<RMCDeployedEntityComponent>();
                int found = 0;
                while (enumerator.MoveNext(out var entity, out var childComp))
                {
                    if (childComp.OriginalEntity != comp.OriginalEntity)
                        continue;
                    if (childComp.SetupIndex == comp.SetupIndex)
                        continue;
                    var mode = origComp.DeploySetups[childComp.SetupIndex].Mode;
                    Logger.Error($"[ServerDeploy] Found child entity={entity} setupIdx={childComp.SetupIndex} mode={mode}");
                    if (mode == RMCDeploySetupMode.ReactiveParental || mode == RMCDeploySetupMode.Reactive)
                    {
                        toDelete.Add(entity);
                    }
                    found++;
                }
                Logger.Error($"[ServerDeploy] Total found children: {found}, to delete: {toDelete.Count}");
                foreach (var entity in toDelete)
                {
                    Logger.Error($"[ServerDeploy] Deleting entity {entity}");
                    EntityManager.DeleteEntity(entity);
                }
            }
        }
    }

    private void OnDeployableShutdown(EntityUid uid, RMCDeployableComponent comp, ComponentShutdown args)
    {
        Logger.Error($"[ServerDeploy] Shutdown original entity {uid}, удаляем все дочерние deploy-сущности");
        var toDelete = new List<EntityUid>();
        var enumerator = EntityManager.EntityQueryEnumerator<RMCDeployedEntityComponent>();
        while (enumerator.MoveNext(out var entity, out var childComp))
        {
            if (childComp.OriginalEntity != uid)
                continue;
            if (childComp.SetupIndex < 0 || childComp.SetupIndex >= comp.DeploySetups.Count)
                continue;
            var setup = comp.DeploySetups[childComp.SetupIndex];
            if (setup.StorageOriginalEntity) //it is already stored inside the entity with such a flag, the entity itself will be deleted soon after that
            {
                childComp.InShutdown = true; // this will really work only in the process of deleting the entity that stores the original entity, in other cases it does not matter
                Dirty(entity, childComp);
                continue;
            }

            if (setup.Mode == RMCDeploySetupMode.ReactiveParental || setup.Mode == RMCDeploySetupMode.Reactive)
            {
                if (childComp.InShutdown)
                {
                    Logger.Error($"[ServerDeploy] Already in shutdown for {uid}");
                    return;
                }

                childComp.InShutdown = true;
                Dirty(entity, childComp);
                toDelete.Add(entity);
            }
        }
        Logger.Error($"[ServerDeploy] Total to delete: {toDelete.Count}");
        foreach (var entity in toDelete)
        {
            Logger.Error($"[ServerDeploy] Deleting entity {entity}");
            EntityManager.DeleteEntity(entity);
        }
    }
}
