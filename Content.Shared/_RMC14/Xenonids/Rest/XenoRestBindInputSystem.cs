using Content.Shared._RMC14.Input;
using Content.Shared.Actions;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rest;

public sealed class XenoRestBindInputSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCRest,
                InputCmdHandler.FromDelegate(session =>
                    {
                        var ent = session?.AttachedEntity;
                        if (ent != null && HasComp<XenoComponent>(ent.Value))
                        {
                            const string restActionPrototypeId = "ActionXenoRest";

                            if (TryFindActionByPrototype(ent.Value,
                                    restActionPrototypeId,
                                    out var actionId,
                                    out var comp))
                            {
                                _actions.PerformAction(
                                    ent.Value,
                                    null,
                                    actionId,
                                    comp,
                                    null,
                                    _timing.CurTime
                                );
                            }
                        }
                    },
                    handle: false
                ))
            .Register<XenoRestBindInputSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoRestBindInputSystem>();
    }

    private bool TryFindActionByPrototype(EntityUid owner,
        string prototypeId,
        out EntityUid actionId,
        out BaseActionComponent comp)
    {
        foreach (var (actId, actComp) in _actions.GetActions(owner))
        {
            var meta = _entMan.GetComponent<MetaDataComponent>(actId);
            if (meta.EntityPrototype != null && meta.EntityPrototype.ID == prototypeId)
            {
                actionId = actId;
                comp = actComp;
                return true;
            }
        }
        actionId = default;
        comp = default!;
        return false;
    }
}
