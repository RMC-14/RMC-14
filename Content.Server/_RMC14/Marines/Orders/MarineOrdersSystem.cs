using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Marines.Orders;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Marines.Orders;

public sealed class MarineOrdersSystem : SharedMarineOrdersSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineOrdersComponent, MapInitEvent>(OnOrdersMapInit);
        SubscribeLocalEvent<MarineOrdersComponent, ComponentShutdown>(OnOrdersShutdown);
    }

    private void OnOrdersMapInit(Entity<MarineOrdersComponent> ent, ref MapInitEvent ev)
    {
        var comp = ent.Comp;

        // All the SetUseDelay calls are required because even tho we set the cooldown on all of them once an order
        // is issued for some reason the order that was pressed uses its delays and does not care about its cooldown
        // being set.
        _actions.AddAction(ent, ref comp.MoveActionEntity, comp.MoveAction);
        _actions.SetUseDelay(comp.MoveActionEntity, comp.Cooldown);
        _actions.AddAction(ent, ref comp.HoldActionEntity, comp.HoldAction);
        _actions.SetUseDelay(comp.HoldActionEntity, comp.Cooldown);
        _actions.AddAction(ent, ref comp.FocusActionEntity, comp.FocusAction);
        _actions.SetUseDelay(comp.FocusActionEntity, comp.Cooldown);
    }

    private void OnOrdersShutdown(Entity<MarineOrdersComponent> ent, ref ComponentShutdown ev)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.FocusActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.HoldActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.MoveActionEntity);
    }

    protected override void OnAction(Entity<MarineOrdersComponent> ent, ref MoveActionEvent ev)
    {
        base.OnAction(ent, ref ev);
        OnAction(ent, ent.Comp.MoveCallouts);
    }

    protected override void OnAction(Entity<MarineOrdersComponent> ent, ref HoldActionEvent ev)
    {
        base.OnAction(ent, ref ev);
        OnAction(ent, ent.Comp.HoldCallouts);
    }

    protected override void OnAction(Entity<MarineOrdersComponent> ent, ref FocusActionEvent ev)
    {
        base.OnAction(ent, ref ev);
        OnAction(ent, ent.Comp.FocusCallouts);
    }

    private void OnAction(EntityUid uid, List<LocId> callouts)
    {
        if (callouts.Count == 0)
            return;

        var callout = _random.Next(0, callouts.Count);
        _chat.TrySendInGameICMessage(uid, Loc.GetString(callouts[callout]), InGameICChatType.Speak, false);
    }
}
