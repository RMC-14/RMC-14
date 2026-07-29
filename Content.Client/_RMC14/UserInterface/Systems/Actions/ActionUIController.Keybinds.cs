using Content.Shared.Actions.Components;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    /// <summary>
    /// Triggers an owned action through the same validation and targeting path as the action UI.
    /// </summary>
    public bool TryTriggerRMCAction(EntityUid actionId)
    {
        if (_actionsSystem?.GetAction(actionId) is not { } action ||
            _playerManager.LocalEntity is not { } user ||
            action.Comp.AttachedEntity != user ||
            !_actionsSystem.ValidAction(action))
        {
            return false;
        }

        if (EntityManager.TryGetComponent<TargetActionComponent>(actionId, out var target))
            ToggleTargeting((actionId, action.Comp, target));
        else
            _actionsSystem.TriggerAction(action);

        return true;
    }
}
