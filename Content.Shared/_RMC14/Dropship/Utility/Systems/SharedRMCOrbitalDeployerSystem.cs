using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Coordinates;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public abstract class SharedRMCOrbitalDeployerSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedSupplyDropSystem SupplyDrop = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    ///     Tries to paradrop an entity to the target's coordinates.
    /// </summary>
    /// <param name="deployer">The entity being deployed, or the entity deciding what to deploy</param>
    /// <param name="target">The target to deploy on</param>
    /// <param name="user">The entity attempting to deploy</param>
    /// <param name="deployerComp">The <see cref="RMCOrbitalDeployerComponent"/></param>
    /// <returns>True if deploying was successful</returns>
    public bool TryDeploy(EntityUid deployer, EntityUid target, EntityUid user, RMCOrbitalDeployerComponent? deployerComp = null)
    {
        if (!Resolve(deployer, ref deployerComp, false))
            return false;

        if (!Container.TryGetContainer(Transform(deployer).ParentUid, deployerComp.DeployableContainerSlotId, out var container))
            return false;

        var deployableEnt = container.ContainedEntities.Count > 0 ? container.ContainedEntities[0] : default;

        if (!TryComp(deployableEnt, out RMCOrbitalDeployableComponent? deployable))
            return false;

        var dropLocation =_map.AlignToGrid(target.ToCoordinates());

        foreach (var defense in _entityLookup.GetEntitiesInRange<TurretComponent>(_transform.ToMapCoordinates(dropLocation), deployable.DefenseExclusionRange))
        {
            var msg = Loc.GetString("rmc-sentry-too-close", ("defense", defense));
            _popup.PopupPredictedCursor(msg, user, PopupType.SmallCaution);
            return false;
        }

        var deploying = deployableEnt;
        if (deployable.DeployPrototype is { } deployPrototype)
        {
            if (deployable.RemainingDeployCount <= 0)
                return false;

            if (_net.IsServer)
            {
                var deployingEntity = Spawn(deployPrototype);
                deploying = deployingEntity;
            }

            deployable.RemainingDeployCount--;
            Dirty(deployableEnt, deployable);
        }

        var openAt = TimeSpan.FromSeconds(deployable.ArrivingSoundDelay + deployable.DropDuration);
        var landingDamage = deployable.LandingDamage;

        if (deployable.DropPod)
        {
            var dropPod = Spawn(deployerComp.DropPodPrototype);
            var podComponent = EnsureComp<SupplyDropPodComponent>(dropPod);
            var podContainer = Container.EnsureContainer<Container>(dropPod, podComponent.DeploySlotId);
            Container.Insert(deploying, podContainer);

            deploying = dropPod;
            openAt += podComponent.OpenTimeRemaining;
            landingDamage = podComponent.LandingDamage;
        }

        _audio.PlayPredicted(deployerComp.LaunchSound, _transform.GetMoverCoordinates(deployer), user);
        SupplyDrop.LaunchSupplyDrop(deploying,
            _transform.ToMapCoordinates(dropLocation),
            deployable.ArrivingSoundDelay,
            deployable.DropDuration,
            openAt,
            landingDamage,
            deployable.LandingEffectId,
            deployable.ArrivingSound,
            deployerComp.DropScatter);

        return true;
    }
}
