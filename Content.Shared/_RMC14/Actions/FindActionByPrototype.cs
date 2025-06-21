using Content.Shared.Actions;

namespace Content.Shared._RMC14.Actions;

public sealed class FindActionByPrototype : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public bool TryFindActionByPrototype(EntityUid owner,
        string prototypeId,
        out EntityUid actionId,
        out BaseActionComponent comp)
    {
        foreach (var (actId, actComp) in _actionsSystem.GetActions(owner))
        {
            if (!EntityManager.TryGetComponent<MetaDataComponent>(actId, out var meta) ||
                meta.EntityPrototype == null || meta.EntityPrototype.ID != prototypeId)
                continue;
            actionId = actId;
            comp = actComp;
            return true;
        }
        actionId = default;
        comp = default!;
        return false;
    }
}
