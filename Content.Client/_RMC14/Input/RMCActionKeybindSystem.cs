using Content.Client.Actions;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Actions.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Input;

public sealed class RMCActionKeybindSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private SimpleRadialMenu? _orderMenu;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCMarineIssueOrder,
                InputCmdHandler.FromDelegate(session => OpenOrderMenu(session?.AttachedEntity), handle: false))
            .Bind(CMKeyFunctions.RMCMarineIssueOrderFocus,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineOrderFocus),
                    handle: false))
            .Bind(CMKeyFunctions.RMCMarineIssueOrderHold,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineOrderHold),
                    handle: false))
            .Bind(CMKeyFunctions.RMCMarineIssueOrderMove,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineOrderMove),
                    handle: false))
            .Bind(CMKeyFunctions.RMCMarineSpecialistOne,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineSpecialistOne),
                    handle: false))
            .Bind(CMKeyFunctions.RMCMarineSpecialistTwo,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineSpecialistTwo),
                    handle: false))
            .Bind(CMKeyFunctions.RMCMarineCycleHelmetHud,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineCycleHelmetHud),
                    handle: false))
            .Bind(CMKeyFunctions.RMCToggleIff,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.MarineToggleIff),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPrimaryActionOne,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPrimaryOne),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPrimaryActionTwo,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPrimaryTwo),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPrimaryActionThree,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPrimaryThree),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPrimaryActionFour,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPrimaryFour),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPrimaryActionFive,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPrimaryFive),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoCorrosiveAcid,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoCorrosiveAcid),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoEvolve,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoEvolve),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoHide,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoHide),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPheromones,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoPheromones),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPheromonesFrenzy,
                InputCmdHandler.FromDelegate(
                    _ => RaiseNetworkEvent(new XenoPheromonesKeybindEvent(XenoPheromones.Frenzy)),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPheromonesRecovery,
                InputCmdHandler.FromDelegate(
                    _ => RaiseNetworkEvent(new XenoPheromonesKeybindEvent(XenoPheromones.Recovery)),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPheromonesWarding,
                InputCmdHandler.FromDelegate(
                    _ => RaiseNetworkEvent(new XenoPheromonesKeybindEvent(XenoPheromones.Warding)),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoPurchaseStrain,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoEvolve),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoScreech,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoScreech),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoTailStab,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoTailStab),
                    handle: false))
            .Bind(CMKeyFunctions.RMCXenoWordQueen,
                InputCmdHandler.FromDelegate(
                    session => TryTrigger(session?.AttachedEntity, RMCKeybindActionSlot.XenoWordQueen),
                    handle: false))
            .Register<RMCActionKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCActionKeybindSystem>();
    }

    public EntityUid? FindAction(EntityUid actor, RMCKeybindActionSlot slot)
    {
        if (!TryComp(actor, out RMCKeybindActionsComponent? mappings) ||
            !mappings.Actions.TryGetValue(slot, out var candidates))
        {
            return null;
        }

        var actions = _actions.GetClientActions();
        foreach (var candidate in candidates)
        {
            foreach (var action in actions)
            {
                if (action.Comp.AttachedEntity == actor &&
                    MetaData(action).EntityPrototype?.ID == candidate.Id)
                {
                    return action;
                }
            }
        }

        return null;
    }

    private void OpenOrderMenu(EntityUid? actor)
    {
        if (actor is not { } user)
            return;

        var actions = new List<RadialMenuOption>();
        AddOrderOption(user, RMCKeybindActionSlot.MarineOrderMove, actions);
        AddOrderOption(user, RMCKeybindActionSlot.MarineOrderHold, actions);
        AddOrderOption(user, RMCKeybindActionSlot.MarineOrderFocus, actions);
        if (actions.Count == 0)
            return;

        _orderMenu?.Close();
        _orderMenu = new SimpleRadialMenu();
        _orderMenu.SetButtons(actions);
        _orderMenu.Track(user);
        _orderMenu.OpenOverMouseScreenPosition();
    }

    private void AddOrderOption(
        EntityUid actor,
        RMCKeybindActionSlot slot,
        ICollection<RadialMenuOption> options)
    {
        if (FindAction(actor, slot) is not { } action ||
            !TryComp(action, out ActionComponent? actionComponent))
        {
            return;
        }

        options.Add(new RadialMenuActionOption<EntityUid>(TryTriggerAction, action)
        {
            Sprite = actionComponent.Icon,
            ToolTip = Name(action),
        });
    }

    private void TryTrigger(EntityUid? actor, RMCKeybindActionSlot slot)
    {
        if (actor is { } user && FindAction(user, slot) is { } action)
            TryTriggerAction(action);
    }

    private void TryTriggerAction(EntityUid action)
    {
        _ui.GetUIController<ActionUIController>().TryTriggerRMCAction(action);
    }
}
