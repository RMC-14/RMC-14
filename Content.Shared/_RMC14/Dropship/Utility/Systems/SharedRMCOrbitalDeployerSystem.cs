using System.Linq;
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

    protected static readonly EntProtoId DefaultDropPodPrototype = "RMCSupplyDropPod";

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
            foreach (var defense in _entityLookup.GetEntitiesInRange(_transform.ToMapCoordinates(dropLocation), deployable.DefenseExclusionRange, LookupFlags.Uncontained))
            {
                if (!Transform(defense).Anchored)
                    continue;

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

    protected bool TryCreateOrbitalDropPod(float timeToOpen, out EntityUid dropPod)
    {
        dropPod = default;
        if (!float.IsFinite(timeToOpen) || timeToOpen < 0)
            return false;

        var pod = Spawn(DefaultDropPodPrototype);
        if (!TryComp(pod, out SupplyDropPodComponent? podComponent))
        {
            QueueDel(pod);
            return false;
        }

        podComponent.OpenTimeRemaining = TimeSpan.FromSeconds(timeToOpen);
        Dirty(pod, podComponent);
        dropPod = pod;
        return true;
    }

    protected void LaunchOrbitalDropPod(EntityUid dropPod,
        MapCoordinates dropLocation,
        float skyFallDuration,
        float dropDuration,
        bool useParachute,
        IReadOnlyList<EntityCoordinates>? launchCoordinates = null,
        int dropScatter = 0)
    {
        if (!TryComp(dropPod, out SupplyDropPodComponent? podComponent))
            return;

        if (launchCoordinates != null)
        {
            foreach (var launch in launchCoordinates.Distinct())
            {
                _audio.PlayPvs(podComponent.LaunchSound, launch);
            }
        }

        var openAt = TimeSpan.FromSeconds(skyFallDuration + dropDuration) + podComponent.OpenTimeRemaining;
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

        _audio.PlayPvs(podComponent.LaunchSound, _transform.GetMoverCoordinates(dropPod));
    }
}
