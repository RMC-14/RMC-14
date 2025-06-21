using Content.Client.Actions;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Input;
using Robust.Shared.Input.Binding;
using Robust.Client.Player;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Rest;

public sealed class XenoRestKeybindSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly FindActionByPrototype _findActionByPrototype = default!;

    private bool _pendingRestTrigger;
    private TimeSpan _pendingRestTriggerUntil = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCXenoRest,
                InputCmdHandler.FromDelegate(_ =>
                {
                    var timing = IoCManager.Resolve<IGameTiming>();
                    var curTime = timing.CurTime;
                    if (_pendingRestTrigger && curTime < _pendingRestTriggerUntil)
                        return;
                    var ent = _playerManager.LocalEntity;
                    if (ent == null || !ent.Value.IsValid() || !HasComp<XenoComponent>(ent.Value))
                        return;
                    if (!_findActionByPrototype.TryFindActionByPrototype(ent.Value, "ActionXenoRest", out var actionId, out var actionComp))
                        return;
                    if (!actionComp.Enabled || actionComp.AttachedEntity != ent.Value)
                        return;
                    if (actionComp.Cooldown.HasValue && actionComp.Cooldown.Value.End > curTime)
                        return;
                    var nextAllowed = curTime + TimeSpan.FromSeconds(0.5);
                    if (actionComp.Cooldown.HasValue && actionComp.Cooldown.Value.End > nextAllowed)
                        nextAllowed = actionComp.Cooldown.Value.End;
                    _pendingRestTrigger = true;
                    _pendingRestTriggerUntil = nextAllowed;
                    _actionsSystem.TriggerAction(actionId, actionComp);
                },
                handle: true))
            .Register<XenoRestKeybindSystem>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var timing = IoCManager.Resolve<IGameTiming>();
        if (_pendingRestTrigger && timing.CurTime >= _pendingRestTriggerUntil)
            _pendingRestTrigger = false;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoRestKeybindSystem>();
    }
}
