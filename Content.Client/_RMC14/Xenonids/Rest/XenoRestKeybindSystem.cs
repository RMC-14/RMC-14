using Content.Client.Actions;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Rest;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Rest;

public sealed class XenoRestKeybindSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCXenoRest,
                InputCmdHandler.FromDelegate(_ =>
                {
                    var ent = _playerManager.LocalEntity;
                    if (ent == null || !ent.Value.IsValid() || !HasComp<XenoComponent>(ent.Value))
                        return;

                    foreach (var (actionId, actionComp) in _rmcActions.GetActionsWithEvent<XenoRestActionEvent>(ent.Value))
                    {
                        if (actionComp is not { Enabled: true } || actionComp.AttachedEntity != ent.Value)
                            continue;

                        if (actionComp.Cooldown.HasValue && actionComp.Cooldown.Value.End > _timing.CurTime)
                            continue;

                        _actionsSystem.TriggerAction((actionId, actionComp));
                        break;
                    }
                },
                handle: true))
            .Register<XenoRestKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoRestKeybindSystem>();
    }
}
