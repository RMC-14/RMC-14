using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Coordinates;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private static readonly EntProtoId DefaultDropPodPrototype = "RMCSupplyDropPod";

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

        if (deployable.DeployBlacklist is { } blacklist)
        {
            foreach (var defense in _entityLookup.GetEntitiesInRange(_transform.ToMapCoordinates(dropLocation), deployable.DefenseExclusionRange))
            {
                if (!_whitelist.IsValid(blacklist, defense))
                    continue;

                var msg = Loc.GetString("rmc-sentry-too-close", ("defense", defense));
                _popup.PopupPredictedCursor(msg, user, PopupType.SmallCaution);
                return false;
            }
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
        var arrivingSound = deployable.ArrivingSound;

        if (deployable.DropPod)
        {
            var dropPod = Spawn(deployerComp.DropPodPrototype);
            DebugTools.Assert(HasComp<SupplyDropPodComponent>(dropPod));

            if (!TryComp(dropPod, out SupplyDropPodComponent? podComponent))
                return false;

            var podContainer = Container.EnsureContainer<Container>(dropPod, podComponent.DeploySlotId);
            Container.Insert(deploying, podContainer);

            deploying = dropPod;
            openAt += podComponent.OpenTimeRemaining;
            landingDamage = podComponent.LandingDamage;
            arrivingSound = podComponent.ArrivingSound;
        }

        _audio.PlayPredicted(deployerComp.LaunchSound, _transform.GetMoverCoordinates(deployer), user);
        SupplyDrop.LaunchSupplyDrop(deploying,
            _transform.ToMapCoordinates(dropLocation),
            deployable.ArrivingSoundDelay,
            deployable.DropDuration,
            openAt,
            landingDamage,
            deployable.LandingEffectId,
            arrivingSound,
            deployerComp.DropScatter,
            deployable.UseParachute);

        return true;
    }

    /// <summary>
    ///     Puts an entity in a drop pod and supply drops it to the given coordinates.
    /// </summary>
    /// <param name="deploying">The entity being deployed.</param>
    /// <param name="dropLocation">The location the drop pod should land at.</param>
    /// <param name="skyFallDuration">How long it should take before the drop pod appears at the target map and starts it's falling animation.</param>
    /// <param name="dropDuration">The duration of the falling animation.</param>
    /// <param name="timeToOpen">The amount of time in seconds it takes after landing for the drop pod to release it's contents.</param>
    /// <param name="dropScatter">How far away from the given drop location the drop pod can be randomly dropped to.</param>
    /// <param name="useParachute">Whether the drop pod should have a parachute during it's falling animation.</param>
    public void DoOrbitalDeploy(EntityUid deploying, MapCoordinates dropLocation, float skyFallDuration = 5, float dropDuration = 3, float timeToOpen = 2, int dropScatter = 0, bool useParachute = true)
    {
        var dropPod = Spawn(DefaultDropPodPrototype);
        DebugTools.Assert(HasComp<SupplyDropPodComponent>(dropPod));

        if (!TryComp(dropPod, out SupplyDropPodComponent? podComponent))
            return;

        _audio.PlayPvs(podComponent.LaunchSound, _transform.GetMoverCoordinates(deploying)); // Play sound at the location the entity is deployed from.

        var openAt = TimeSpan.FromSeconds(skyFallDuration + dropDuration + timeToOpen);
        var podContainer = Container.EnsureContainer<Container>(dropPod, podComponent.DeploySlotId);
        Container.Insert(deploying, podContainer);

        _audio.PlayPvs(podComponent.LaunchSound, _transform.GetMoverCoordinates(deploying)); // Play sound at the location of the entity after being inserted into the drop pod.

        SupplyDrop.LaunchSupplyDrop(dropPod,
            _transform.ToMapCoordinates(_map.AlignToGrid(_transform.ToCoordinates(dropLocation))),
            skyFallDuration,
            dropDuration,
            openAt,
            podComponent.LandingDamage,
            podComponent.LandingEffectId,
            podComponent.ArrivingSound,
            dropScatter,
            useParachute);
    }
}
