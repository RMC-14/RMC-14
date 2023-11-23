using Robust.Shared.Physics.Systems;

namespace Content.Shared._CM14.Xenos.Hide;

public sealed class XenoHideSystem : EntitySystem
{
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

        _appearance.SetData(xeno, XenoVisualLayers.Hide, xeno.Comp.Hiding);
    }
}
