using Content.Shared.Actions;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Xenonids.Zoom;

public sealed class XenoZoomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomActionEvent>(OnXenoZoomAction);
    }

    private void OnXenoZoomAction(Entity<XenoZoomComponent> xeno, ref XenoZoomActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        xeno.Comp.Enabled = !xeno.Comp.Enabled;
        Dirty(xeno);

        if (xeno.Comp.Enabled)
        {
            _eye.SetMaxZoom(xeno, xeno.Comp.Zoom);
            _eye.SetZoom(xeno, xeno.Comp.Zoom);
        }
        else
        {
            _eye.ResetZoom(xeno);
        }

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoZoomActionEvent)
                _actions.SetToggled(actionId, xeno.Comp.Enabled);
        }
    }
}
