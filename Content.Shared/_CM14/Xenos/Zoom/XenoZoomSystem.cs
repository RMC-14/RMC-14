using System.Numerics;
using Content.Shared.Movement.Systems;

namespace Content.Shared._CM14.Xenos.Zoom;

public sealed class XenoZoomSystem : EntitySystem
{
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
            return;
        }

        _eye.ResetZoom(xeno);
    }
}
