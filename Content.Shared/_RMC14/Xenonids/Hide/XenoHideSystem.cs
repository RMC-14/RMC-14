using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.Hide;

public sealed class XenoHideSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

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

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoHideActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), xeno.Comp.Hiding);
        }

        _appearance.SetData(xeno, XenoVisualLayers.Hide, xeno.Comp.Hiding);
    }
}
