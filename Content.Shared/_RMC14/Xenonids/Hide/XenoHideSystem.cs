using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.Hide;

public sealed class XenoHideSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHideComponent, XenoHideActionEvent>(OnXenoHideAction);
    }

    private void OnXenoHideAction(Entity<XenoHideComponent> xeno, ref XenoHideActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        xeno.Comp.Hiding = !xeno.Comp.Hiding;
        Dirty(xeno);

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoHideActionEvent)
                _actions.SetToggled(actionId, xeno.Comp.Hiding);
        }

        _appearance.SetData(xeno, XenoVisualLayers.Hide, xeno.Comp.Hiding);
    }
}
