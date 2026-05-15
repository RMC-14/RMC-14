// ReSharper disable CheckNamespace

using System.Linq;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.CombatMode;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    private static readonly EntProtoId ActionVehicleToggleViewId = "ActionVehicleToggleView";
    private static readonly EntProtoId ActionVehicleLockId = "ActionVehicleLock";

    private readonly List<EntityUid?> _vehicleActions = new();
    private bool _vehicleHotbarOverride;

    private IReadOnlyList<EntityUid?> GetActiveHotbarActions()
    {
        return _vehicleHotbarOverride ? _vehicleActions : _actions;
    }

    private List<EntityUid?> GetEditableHotbarActions()
    {
        return _vehicleHotbarOverride ? _vehicleActions : _actions;
    }

    private void RefreshVehicleHotbarOverride(bool forceUpdate = false)
    {
        if (_actionsSystem == null)
            return;

        var hasVehicleContext = HasVehicleHotbarContext();
        var shouldOverride = ShouldUseVehicleHotbarOverride();
        var changed = false;

        if (!shouldOverride && !_vehicleHotbarOverride && !forceUpdate)
            return;

        if (shouldOverride)
        {
            if (!_vehicleHotbarOverride)
            {
                _vehicleHotbarOverride = true;
                changed = true;
            }

            if (RebuildVehicleActionList())
                changed = true;
        }
        else if (_vehicleHotbarOverride)
        {
            _vehicleHotbarOverride = false;
            if (!hasVehicleContext)
                _vehicleActions.Clear();
            changed = true;
        }
        else if (!hasVehicleContext && _vehicleActions.Count > 0)
        {
            _vehicleActions.Clear();
        }

        if (changed || forceUpdate)
            _container?.SetActionData(_actionsSystem, GetActiveHotbarActions().ToArray());
    }

    private bool ShouldUseVehicleHotbarOverride()
    {
        if (_playerManager.LocalEntity is not { } user)
            return false;

        return EntityManager.HasComponent<VehicleWeaponsOperatorComponent>(user);
    }

    private bool HasVehicleHotbarContext()
    {
        if (_playerManager.LocalEntity is not { } user)
            return false;

        return EntityManager.HasComponent<VehicleWeaponsOperatorComponent>(user);
    }

    private bool RebuildVehicleActionList()
    {
        if (_actionsSystem == null)
            return false;

        var old = _vehicleActions.ToList();
        var desiredOrdered = new List<EntityUid?>();
        var includeHardpointActions = true;

        if (_playerManager.LocalEntity is { } localUser &&
            EntityManager.TryGetComponent<VehicleViewToggleComponent>(localUser, out var viewToggle))
        {
            includeHardpointActions = viewToggle.IsOutside;
        }

        if (!includeHardpointActions)
        {
            foreach (var action in _actions)
            {
                if (action is not { } actionUid ||
                    EntityManager.HasComponent<VehicleHardpointActionComponent>(actionUid))
                {
                    continue;
                }

                desiredOrdered.Add(actionUid);
            }
        }
        else if (_playerManager.LocalEntity is { } user &&
                 EntityManager.TryGetComponent<CombatModeComponent>(user, out var combat) &&
                 combat.CombatToggleActionEntity is { } combatAction &&
                 !EntityManager.HasComponent<VehicleHardpointActionComponent>(combatAction))
        {
            desiredOrdered.Add(combatAction);
        }

        if (includeHardpointActions)
        {
            var hardpointActions = _actionsSystem
                .GetClientActions()
                .Where(action => EntityManager.TryGetComponent<VehicleHardpointActionComponent>(action, out _))
                .OrderBy(action =>
                {
                    EntityManager.TryGetComponent<VehicleHardpointActionComponent>(action, out var hardpointAction);
                    return hardpointAction?.SortOrder ?? 0;
                });

            foreach (var (uid, _) in hardpointActions)
            {
                desiredOrdered.Add(uid);
            }
        }

        var viewToggleAction = FindVehicleUtilityAction(
            ActionVehicleToggleViewId,
            user => EntityManager.TryGetComponent<VehicleViewToggleComponent>(user, out var toggleState)
                    && toggleState.Action is { } viewAction
                    && !EntityManager.HasComponent<VehicleHardpointActionComponent>(viewAction)
                ? viewAction
                : null);

        if (viewToggleAction is { } ensuredViewAction)
            desiredOrdered.Add(ensuredViewAction);

        var vehicleLockAction = FindVehicleUtilityAction(
            ActionVehicleLockId,
            user => EntityManager.TryGetComponent<VehicleLockActionComponent>(user, out var lockState)
                    && lockState.Action is { } lockAction
                    && !EntityManager.HasComponent<VehicleHardpointActionComponent>(lockAction)
                ? lockAction
                : null);

        if (vehicleLockAction is { } ensuredVehicleLockAction)
            desiredOrdered.Add(ensuredVehicleLockAction);

        // Preserve manual ordering while all current actions still exist.
        var remaining = new HashSet<EntityUid>();
        foreach (var desired in desiredOrdered)
        {
            if (desired is { } uid)
                remaining.Add(uid);
        }

        var rebuilt = new List<EntityUid?>();
        foreach (var existing in _vehicleActions)
        {
            if (existing is not { } existingUid || !remaining.Remove(existingUid))
                continue;

            rebuilt.Add(existingUid);
        }

        foreach (var desired in desiredOrdered)
        {
            if (desired is not { } desiredUid || !remaining.Remove(desiredUid))
                continue;

            rebuilt.Add(desiredUid);
        }

        _vehicleActions.Clear();
        _vehicleActions.AddRange(rebuilt);

        if (old.Count != _vehicleActions.Count)
            return true;

        for (var i = 0; i < old.Count; i++)
        {
            if (old[i] != _vehicleActions[i])
                return true;
        }

        return false;
    }

    private EntityUid? FindVehicleUtilityAction(EntProtoId actionPrototype, Func<EntityUid, EntityUid?> componentAction)
    {
        if (_actionsSystem == null)
            return null;

        if (_playerManager.LocalEntity is { } user &&
            componentAction(user) is { } existing)
        {
            return existing;
        }

        foreach (var action in _actionsSystem.GetClientActions())
        {
            var uid = action.Owner;
            if (EntityManager.HasComponent<VehicleHardpointActionComponent>(uid) ||
                !MatchesActionPrototype(uid, actionPrototype))
            {
                continue;
            }

            return uid;
        }

        return null;
    }

    private bool MatchesActionPrototype(EntityUid actionUid, EntProtoId prototype)
    {
        return EntityManager.TryGetComponent(actionUid, out MetaDataComponent? metaData) &&
               metaData.EntityPrototype?.ID == prototype.ToString();
    }
}
