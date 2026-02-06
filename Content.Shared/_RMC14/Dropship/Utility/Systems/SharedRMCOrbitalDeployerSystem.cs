using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared.Coordinates;
using Content.Shared.ParaDrop;
using Robust.Shared.Containers;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public abstract class SharedRMCOrbitalDeployerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <summary>
    ///     Tries to paradrop an entity to the target's coordinates.
    /// </summary>
    /// <param name="deployer">The entity being deployed, or the entity deciding what to deploy</param>
    /// <param name="target">The target to deploy on</param>
    /// <param name="deployerComp">The <see cref="RMCOrbitalDeployerComponent"/></param>
    /// <returns>True if deploying was successful</returns>
    public bool TryDeploy(EntityUid deployer, EntityUid target, RMCOrbitalDeployerComponent? deployerComp = null)
    {
        if (!Resolve(deployer, ref deployerComp, false))
            return false;

        if (!_container.TryGetContainer(Transform(deployer).ParentUid, deployerComp.DeployableContainerSlotId, out var container))
            return false;

        var deployableEnt = container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : default;

        if (!TryComp(deployableEnt, out RMCOrbitalDeployableComponent? deployable))
            return false;

        var deploying = deployableEnt;
        if (deployable.DeployPrototype is { } deployPrototype)
        {
            if (deployable.RemainingDeployCount <= 0)
                return false;

            var deployingEntity = Spawn(deployPrototype);
            deploying = deployingEntity;

            deployable.RemainingDeployCount--;
            Dirty(deployableEnt, deployable);
        }

        Deploy(deploying, target,  deployerComp.DropScatter, deployable, deployable.UseParachute);
        return true;
    }

    private void Deploy(EntityUid deployable, EntityUid target, int dropScatter, RMCOrbitalDeployableComponent deployableComp, bool parachute = true)
    {
        var crashLandable = EnsureComp<CrashLandableComponent>(deployable);
        var travelTime = crashLandable.CrashDuration;
        if (parachute)
        {
            var paraDroppable = EnsureComp<ParaDroppableComponent>(deployable);
            paraDroppable.DropScatter = dropScatter;
            Dirty(deployable, paraDroppable);

            travelTime = paraDroppable.DropDuration;
        }

        var dropLocation =_map.AlignToGrid(target.ToCoordinates());
        if (deployableComp.LandingEffectId != null)
        {
            var deployEffect = Spawn(deployableComp.LandingEffectId, dropLocation);
            var timer = EnsureComp<TimedDespawnComponent>(deployEffect);
            timer.Lifetime = travelTime + SkyFallingComponent.DefaultFallDuration;
        }

        var ev = new AttemptCrashLandEvent(deployable, dropLocation);
        RaiseLocalEvent(deployable, ref ev);
    }
}
