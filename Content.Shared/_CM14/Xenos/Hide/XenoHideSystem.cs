using Robust.Shared.Physics.Systems;

namespace Content.Shared._CM14.Xenos.Hide;

public sealed class XenoHideSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHideComponent, XenoHideActionEvent>(OnXenoHide);
    }

    private void OnXenoHide(Entity<XenoHideComponent> ent, ref XenoHideActionEvent args)
    {
        ent.Comp.Hiding = !ent.Comp.Hiding;
        _appearance.SetData(ent, XenoVisualLayers.Hide, ent.Comp.Hiding);
    }
}
