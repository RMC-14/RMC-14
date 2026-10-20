using Content.Shared.Actions.Components;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    /// <summary>
    /// Routes an owned action through the same targeting and activation paths as the action UI.
    /// </summary>
    public bool TryTriggerRMCAction(EntityUid actionId)
    {
        if (_actionsSystem?.GetAction(actionId) is not { } action ||
            _playerManager.LocalEntity is not { } user ||
            action.Comp.AttachedEntity != user ||
            !action.Comp.Enabled)
        {
            return false;
        }

        if (EntityManager.TryGetComponent<TargetActionComponent>(actionId, out var target))
        {
            ToggleTargeting((actionId, action.Comp, target));
            return true;
        }

        if (!EntityManager.HasComponent<InstantActionComponent>(actionId))
            return false;

        _actionsSystem.TriggerAction(action);
        return true;
    }
}
