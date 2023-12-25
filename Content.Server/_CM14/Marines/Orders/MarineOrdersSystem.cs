using Content.Server.Actions;
using Content.Shared._CM14.Marines.Orders;

namespace Content.Server._CM14.Marines.Orders;

public sealed class MarineOrdersSystem : SharedMarineOrdersSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineOrdersComponent, MapInitEvent>(OnOrdersMapInit);
        SubscribeLocalEvent<MarineOrdersComponent, ComponentShutdown>(OnOrdersShutdown);
    }

    private void OnOrdersMapInit(EntityUid uid, MarineOrdersComponent comp, MapInitEvent ev)
    {
        _actions.AddAction(uid, ref comp.FocusActionEntity, comp.FocusAction);
        _actions.AddAction(uid, ref comp.HoldActionEntity, comp.HoldAction);
        _actions.AddAction(uid, ref comp.MoveActionEntity, comp.MoveAction);
    }

    private void OnOrdersShutdown(EntityUid uid, MarineOrdersComponent comp, ComponentShutdown ev)
    {
        _actions.RemoveAction(uid, comp.FocusActionEntity);
        _actions.RemoveAction(uid, comp.HoldActionEntity);
        _actions.RemoveAction(uid, comp.MoveActionEntity);
    }

}
