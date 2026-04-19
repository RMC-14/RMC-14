using System;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Sentry;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleDeploySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly VehicleSystem _vehicleSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _targeting = default!;
    [Dependency] private readonly SharedGunSystem _guns = default!;
    [Dependency] private readonly VehicleTurretSystem _turret = default!;
    [Dependency] private readonly VehicleTopologySystem _topology = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StrapComponent, StrappedEvent>(OnDriverStrapped);
        SubscribeLocalEvent<StrapComponent, UnstrappedEvent>(OnDriverUnstrapped);
        SubscribeLocalEvent<VehicleDeployActionComponent, VehicleDeployActionEvent>(OnDeployAction);
        SubscribeLocalEvent<VehicleDeployActionComponent, ComponentShutdown>(OnDeployActionShutdown);
        SubscribeLocalEvent<VehicleDeployableComponent, VehicleCanRunEvent>(OnVehicleCanRun);
        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnHardpointSlotsChanged);
        SubscribeLocalEvent<HardpointItemComponent, AttemptShootEvent>(OnDeployableAttemptShoot);
    }

    private void OnDriverStrapped(Entity<StrapComponent> ent, ref StrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<VehicleDriverSeatComponent>(ent.Owner))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        if (!TryComp(vehicle.Value, out VehicleDeployableComponent? deployable))
            return;

        EnableDeployAction(args.Buckle.Owner, vehicle.Value, deployable);
    }

    private void OnDriverUnstrapped(Entity<StrapComponent> ent, ref UnstrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<VehicleDriverSeatComponent>(ent.Owner))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        DisableDeployAction(args.Buckle.Owner, vehicle.Value);
    }

    private void EnableDeployAction(EntityUid user, EntityUid vehicle, VehicleDeployableComponent deployable)
    {
        var actionComp = EnsureComp<VehicleDeployActionComponent>(user);
        actionComp.Vehicle = vehicle;

        if (actionComp.Action == null)
            actionComp.Action = _actions.AddAction(user, actionComp.ActionId);

        UpdateDeployActionState(user, actionComp, deployable);
        Dirty(user, actionComp);
    }

    private void DisableDeployAction(EntityUid user, EntityUid vehicle)
    {
        if (!TryComp(user, out VehicleDeployActionComponent? actionComp))
            return;

        if (actionComp.Vehicle != vehicle)
            return;

        if (actionComp.Action != null)
            _actions.RemoveAction(user, actionComp.Action.Value);

        RemCompDeferred<VehicleDeployActionComponent>(user);
    }

    private void OnDeployActionShutdown(Entity<VehicleDeployActionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Action != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.Action.Value);
    }

    private void OnDeployAction(Entity<VehicleDeployActionComponent> ent, ref VehicleDeployActionEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;

        if (ent.Comp.Vehicle is not { } vehicle)
            return;

        if (!TryComp(vehicle, out VehicleDeployableComponent? deployable))
            return;

        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) ||
            vehicleComp.Operator != ent.Owner)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-deploy-not-driver"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (deployable.Deploying)
            return;

        var deployingTo = !deployable.Deployed;
        if (deployingTo && !TryGetVehicleTurret(vehicle, out _))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-deploy-requires-turret"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        deployable.Deploying = true;
        deployable.DeployingTo = deployingTo;
        deployable.Deployer = ent.Owner;
        var delay = deployingTo ? deployable.DeployTime : deployable.UndeployTime;
        deployable.DeployEndTime = _timing.CurTime + delay;
        deployable.AutoTarget = null;
        deployable.NextAutoTargetTime = TimeSpan.Zero;
        deployable.AutoSpinInitialized = false;
        Dirty(vehicle, deployable);

        if (!deployingTo && TryGetVehicleTurret(vehicle, out var turretUid))
        {
            var vehicleRot = _transform.GetWorldRotation(vehicle);
            _turret.TrySetTargetRotationWorld(turretUid, vehicleRot);
        }

        UpdateDeployActionState(ent.Owner, ent.Comp, deployable);

        var actionEntity = ent.Comp.Action;
        if (actionEntity != null)
            _actions.SetCooldown(actionEntity.Value, delay);

        var popupKey = deployingTo ? "rmc-vehicle-deploy-start" : "rmc-vehicle-undeploy-start";
        var startMsg = Loc.GetString(popupKey);
        _popup.PopupClient(startMsg, ent.Owner, ent.Owner, PopupType.Small);
        SendDeployChat(ent.Owner, vehicle, startMsg);
    }

    private void OnVehicleCanRun(Entity<VehicleDeployableComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (ent.Comp.Deploying || ent.Comp.Deployed)
            args.CanRun = false;
    }

    private void UpdateDriverActionState(EntityUid vehicle, VehicleDeployableComponent deployable)
    {
        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator == null)
            return;

        var driver = vehicleComp.Operator.Value;
        if (!TryComp(driver, out VehicleDeployActionComponent? actionComp))
            return;

        if (actionComp.Vehicle != vehicle)
            return;

        UpdateDeployActionState(driver, actionComp, deployable);
        Dirty(driver, actionComp);
    }

    private void UpdateDeployActionState(EntityUid user, VehicleDeployActionComponent actionComp, VehicleDeployableComponent deployable)
    {
        if (actionComp.Action == null)
            return;

        var canDeploy = true;
        EntityUid? turretUid = null;
        if (actionComp.Vehicle is { } vehicle)
        {
            var hasTurret = TryGetVehicleTurret(vehicle, out var foundTurret);
            canDeploy = deployable.Deployed || hasTurret;
            turretUid = hasTurret ? foundTurret : null;

            if (actionComp.Action is { } actionEntity && TryComp(actionEntity, out ActionComponent? actionComponent))
                _actions.SetEntityIcon((actionEntity, actionComponent), turretUid ?? vehicle);
        }

        var actionEntityUid = actionComp.Action.Value;
        _actions.SetToggled(actionEntityUid, deployable.Deployed || deployable.Deploying);
        _actions.SetEnabled(actionEntityUid, !deployable.Deploying && canDeploy);

        UpdateDeployActionText(actionEntityUid, deployable);
    }

    private void UpdateDeployActionText(EntityUid action, VehicleDeployableComponent deployable)
    {
        string nameKey;
        string descKey;

        if (deployable.Deploying)
        {
            if (deployable.DeployingTo)
            {
                nameKey = "rmc-vehicle-deploy-action-name-deploying";
                descKey = "rmc-vehicle-deploy-action-desc-deploying";
            }
            else
            {
                nameKey = "rmc-vehicle-deploy-action-name-undeploying";
                descKey = "rmc-vehicle-deploy-action-desc-undeploying";
            }
        }
        else if (deployable.Deployed)
        {
            nameKey = "rmc-vehicle-deploy-action-name-undeploy";
            descKey = "rmc-vehicle-deploy-action-desc-undeploy";
        }
        else
        {
            nameKey = "rmc-vehicle-deploy-action-name-deploy";
            descKey = "rmc-vehicle-deploy-action-desc-deploy";
        }

        _meta.SetEntityName(action, Loc.GetString(nameKey));
        _meta.SetEntityDescription(action, Loc.GetString(descKey));
    }

    private void ClearDriverDeployCooldown(EntityUid vehicle)
    {
        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator == null)
            return;

        var driver = vehicleComp.Operator.Value;
        if (!TryComp(driver, out VehicleDeployActionComponent? actionComp) || actionComp.Action == null)
            return;

        _actions.ClearCooldown(actionComp.Action.Value);
    }

    private void OnHardpointSlotsChanged(HardpointSlotsChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(args.Vehicle, out VehicleDeployableComponent? deployable))
            return;

        UpdateDriverActionState(args.Vehicle, deployable);
    }

    private void OnDeployableAttemptShoot(Entity<HardpointItemComponent> ent, ref AttemptShootEvent args)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (!string.Equals(ent.Comp.HardpointType, "Cannon", StringComparison.OrdinalIgnoreCase))
            return;

        if (!TryGetVehicleFromContained(ent.Owner, out var vehicle))
        {
            args.Cancelled = true;
            args.ResetCooldown = true;
            return;
        }

        if (!TryComp(vehicle, out VehicleDeployableComponent? deployable))
            return;

        if (!TryComp(vehicle, out VehicleDeployGatedHardpointsComponent? gated) ||
            !IsBlockedHardpoint(gated, ent.Comp.HardpointType))
        {
            return;
        }

        if (!deployable.Deployed)
        {
            args.Cancelled = true;
            args.ResetCooldown = true;
        }
    }

    private static bool IsBlockedHardpoint(VehicleDeployGatedHardpointsComponent gated, string hardpointType)
    {
        if (string.IsNullOrWhiteSpace(hardpointType))
            return false;

        foreach (var blocked in gated.BlockedHardpoints)
        {
            if (string.Equals(blocked, hardpointType, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<VehicleDeployableComponent, TransformComponent>();
        while (query.MoveNext(out var vehicle, out var deployable, out _))
        {
            var now = _timing.CurTime;
            if (deployable.Deploying)
            {
                if (now >= deployable.DeployEndTime)
                {
                    var finishedDeploy = deployable.DeployingTo;
                    var deployer = deployable.Deployer;

                    deployable.Deploying = false;
                    deployable.DeployingTo = false;
                    deployable.DeployEndTime = TimeSpan.Zero;
                    deployable.Deployed = finishedDeploy;

                    if (!deployable.Deployed)
                    {
                        deployable.Deployer = null;
                        deployable.TargetingDeployer = null;
                        deployable.AutoTarget = null;
                        deployable.AutoSpinInitialized = false;
                    }

                    Dirty(vehicle, deployable);
                    UpdateDriverActionState(vehicle, deployable);
                    ClearDriverDeployCooldown(vehicle);

                    var popupKey = finishedDeploy ? "rmc-vehicle-deploy-finish" : "rmc-vehicle-undeploy-finish";
                    if (deployer != null)
                    {
                        var finishMsg = Loc.GetString(popupKey);
                        _popup.PopupClient(finishMsg, deployer.Value, deployer.Value, PopupType.Small);
                        SendDeployChat(deployer.Value, vehicle, finishMsg);
                    }
                    else
                        _popup.PopupEntity(Loc.GetString(popupKey), vehicle, PopupType.Small);
                }

                continue;
            }

            if (!deployable.Deployed || !deployable.AutoTurretEnabled)
            {
                deployable.AutoSpinInitialized = false;
                continue;
            }

            EntityUid? operatorUid = null;
            if (TryComp(vehicle, out VehicleWeaponsComponent? weapons))
                operatorUid = weapons.Operator;

            if (operatorUid != null && _combatMode.IsInCombatMode(operatorUid))
            {
                deployable.AutoSpinInitialized = false;
                continue;
            }

            if (!TryFindAutoGun(vehicle, out var gunUid, out var gunComp))
                continue;

            if (deployable.AutoTarget != null &&
                !IsValidAutoTarget(vehicle, deployable, deployable.AutoTarget.Value, deployable.AutoTargetRange))
            {
                deployable.AutoTarget = null;
                deployable.NextAutoTargetTime = TimeSpan.Zero;
                deployable.AutoSpinInitialized = false;
            }

            if (deployable.AutoTarget == null &&
                (deployable.NextAutoTargetTime == TimeSpan.Zero || now >= deployable.NextAutoTargetTime))
            {
                deployable.NextAutoTargetTime = now + TimeSpan.FromSeconds(Math.Max(0f, deployable.AutoTargetCooldown));
                deployable.AutoTarget = FindAutoTarget(vehicle, deployable, deployable.AutoTargetRange);
            }

            if (deployable.AutoTarget is { } target)
            {
                deployable.AutoSpinInitialized = false;
                if (!_turret.TryAimAtTarget(gunUid, target, out var targetCoords))
                {
                    deployable.AutoTarget = null;
                    deployable.NextAutoTargetTime = TimeSpan.Zero;
                    continue;
                }

                if (!HasAmmo(gunUid))
                    continue;

                // Mark the shot as targeted so downed entities (RequireProjectileTarget) can be hit.
                var previousTarget = _guns.SwapTarget((gunUid, gunComp), target);
                _guns.AttemptShoot((gunUid, gunComp), vehicle, targetCoords);
                _guns.SwapTarget((gunUid, gunComp), previousTarget);
            }
            else if (deployable.AutoSpinSpeed > 0f)
            {
                if (!deployable.AutoSpinInitialized)
                {
                    deployable.AutoSpinWorldRotation = GetTurretWorldRotation(gunUid, vehicle);
                    deployable.AutoSpinInitialized = true;
                }

                var delta = Angle.FromDegrees(deployable.AutoSpinSpeed * frameTime);
                deployable.AutoSpinWorldRotation = (deployable.AutoSpinWorldRotation + delta).Reduced();
                _turret.TrySetTargetRotationWorld(gunUid, deployable.AutoSpinWorldRotation);
            }
        }
    }

    private bool TryFindAutoGun(EntityUid vehicle, out EntityUid gunUid, out GunComponent gunComp)
    {
        gunUid = default;
        gunComp = default!;

        EntityUid? fallbackGun = null;
        GunComponent? fallbackComp = null;

        foreach (var mountedSlot in _topology.GetMountedSlots(vehicle))
        {
            if (mountedSlot.Item is not { } installed)
                continue;

            if (TryGetGunCandidate(installed, out var directGun, out var directComp))
            {
                if (HasAmmo(directGun))
                {
                    gunUid = directGun;
                    gunComp = directComp;
                    return true;
                }

                fallbackGun ??= directGun;
                fallbackComp ??= directComp;
            }
        }

        if (fallbackGun != null && fallbackComp != null)
        {
            gunUid = fallbackGun.Value;
            gunComp = fallbackComp;
            return true;
        }

        return false;
    }

    private bool TryGetGunCandidate(EntityUid uid, out EntityUid gunUid, out GunComponent gunComp)
    {
        gunUid = uid;
        gunComp = default!;

        if (!TryComp(uid, out GunComponent? gun) || !HasComp<VehicleTurretComponent>(uid))
            return false;

        gunComp = gun;
        return true;
    }

    private bool HasAmmo(EntityUid gunUid)
    {
        if (!HasComp<GunComponent>(gunUid))
            return false;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(gunUid, ref ammoEv);
        return ammoEv.Capacity <= 0 || ammoEv.Count > 0;
    }

    private EntityUid? FindAutoTarget(EntityUid vehicle, VehicleDeployableComponent deployable, float range)
    {
        if (range <= 0f)
            return null;

        var targeting = EnsureComp<SentryTargetingComponent>(vehicle);

        if (deployable.Deployer != null && deployable.Deployer != deployable.TargetingDeployer)
        {
            _targeting.ApplyDeployerFactions(vehicle, deployable.Deployer.Value);
            deployable.TargetingDeployer = deployable.Deployer;
        }

        var vehicleCoords = _transform.GetMapCoordinates(vehicle);
        var bestDistance = float.MaxValue;
        EntityUid? bestTarget = null;

        foreach (var candidate in _targeting.GetNearbyIffHostiles((vehicle, targeting), range))
        {
            if (candidate == vehicle)
                continue;

            if (!IsValidAutoTarget(vehicle, deployable, candidate, range, targeting))
                continue;

            var targetCoords = _transform.GetMapCoordinates(candidate);
            var distance = (targetCoords.Position - vehicleCoords.Position).LengthSquared();
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private bool TryGetVehicleTurret(
        EntityUid vehicle,
        out EntityUid turretUid,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        return _topology.TryGetPrimaryTurret(vehicle, out turretUid, hardpoints, itemSlots);
    }

    private bool TryGetVehicleFromContained(EntityUid contained, out EntityUid vehicle)
    {
        return _topology.TryGetVehicle(contained, out vehicle);
    }

    private bool IsValidAutoTarget(EntityUid vehicle, VehicleDeployableComponent deployable, EntityUid target, float range)
    {
        if (!TryComp(vehicle, out SentryTargetingComponent? targeting))
            targeting = EnsureComp<SentryTargetingComponent>(vehicle);

        return IsValidAutoTarget(vehicle, deployable, target, range, targeting);
    }

    private bool IsValidAutoTarget(
        EntityUid vehicle,
        VehicleDeployableComponent deployable,
        EntityUid target,
        float range,
        SentryTargetingComponent targeting)
    {
        if (deployable.Deployer != null && deployable.Deployer != deployable.TargetingDeployer)
        {
            _targeting.ApplyDeployerFactions(vehicle, deployable.Deployer.Value);
            deployable.TargetingDeployer = deployable.Deployer;
        }

        if (!Exists(target))
            return false;

        if (TryComp(target, out MobStateComponent? mobState) && mobState.CurrentState == MobState.Dead)
            return false;

        if (!_targeting.IsValidTarget((vehicle, targeting), target))
            return false;

        var targetCoords = Transform(target).Coordinates;
        if (TryGetVehicleTurret(vehicle, out var turretUid) &&
            TryComp(turretUid, out VehicleTurretComponent? turret) &&
            _turret.TryGetTurretOrigin(turretUid, turret, out var originCoords))
        {
            var originMap = _transform.ToMapCoordinates(originCoords);
            var targetMap = _transform.ToMapCoordinates(targetCoords);
            if (!_interaction.InRangeUnobstructed(originMap, targetMap, range))
                return false;
        }
        else
        {
            if (!_interaction.InRangeUnobstructed(vehicle, targetCoords, range, popup: false))
                return false;
        }

        return true;
    }

    private Angle GetTurretWorldRotation(EntityUid turretUid, EntityUid vehicle)
    {
        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return _transform.GetWorldRotation(vehicle);

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        return (turret.WorldRotation + vehicleRot).Reduced();
    }

    private void SendDeployChat(EntityUid deployer, EntityUid vehicle, string message)
    {
        if (!TryComp(deployer, out ActorComponent? actor))
            return;

        var name = TryComp(vehicle, out MetaDataComponent? meta)
            ? meta.EntityName
            : Loc.GetString("entity-unknown-name");
        name = FormattedMessage.EscapeText(name);
        var wrappedMessage = Loc.GetString("chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString("chat-speech-verb-default")),
            ("fontType", "Default"),
            ("fontSize", 12),
            ("message", FormattedMessage.EscapeText(message)));

        _rmcChat.ChatMessageToOne(
            ChatChannel.Local,
            message,
            wrappedMessage,
            vehicle,
            hideChat: false,
            actor.PlayerSession.Channel);
    }

}
