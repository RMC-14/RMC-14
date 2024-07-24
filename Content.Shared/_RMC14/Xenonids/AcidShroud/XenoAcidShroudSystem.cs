using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Xenonids.AcidShroud;

public sealed class XenoAcidShroudSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoAcidShroudActionEvent>(OnAcidShroudAction);
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoAcidShroudDoAfterEvent>(OnAcidShroudDoAfter);
    }

    private void OnAcidShroudAction(Entity<XenoAcidShroudComponent> ent, ref XenoAcidShroudActionEvent args)
    {
        var ev = new XenoAcidShroudDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.DoAfter, ev, ent);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnAcidShroudDoAfter(Entity<XenoAcidShroudComponent> ent, ref XenoAcidShroudDoAfterEvent args)
    {

    }
}
