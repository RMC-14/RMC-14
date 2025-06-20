using Content.Client.Actions;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Input;
using Robust.Shared.Input.Binding;
using Robust.Client.Player;
using Content.Shared._RMC14.Xenonids;

namespace Content.Client._RMC14.Xenonids.Rest;

public sealed class XenoRestKeybindSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly FindActionByPrototype _findActionByPrototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCRest,
                InputCmdHandler.FromDelegate(_ =>
                {
                    var ent = _playerManager.LocalEntity;
                    if (ent == null || !ent.Value.IsValid() || !HasComp<XenoComponent>(ent.Value))
                        return;

                    if (!_findActionByPrototype.TryFindActionByPrototype(ent.Value, "ActionXenoRest", out var actionId, out var actionComp))
                        return;

                    if (!actionComp.Enabled || actionComp.AttachedEntity != ent.Value)
                        return;

                    _actionsSystem.TriggerAction(actionId, actionComp);
                },
                handle: false))
            .Register<XenoRestKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoRestKeybindSystem>();
    }
}
