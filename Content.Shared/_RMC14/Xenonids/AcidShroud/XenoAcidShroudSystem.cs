using Content.Shared._RMC14.Actions;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Xenonids.AcidShroud;

public sealed class XenoAcidShroudSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoAcidShroudActionEvent>(OnAcidShroudAction);
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoAcidShroudDoAfterEvent>(OnAcidShroudDoAfter);
    }

    private void OnAcidShroudAction(Entity<XenoAcidShroudComponent> ent, ref XenoAcidShroudActionEvent args)
    {
        args.Handled = true;
        var ev = new XenoAcidShroudDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.DoAfter, ev, ent, args.Action)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnAcidShroudDoAfter(Entity<XenoAcidShroudComponent> ent, ref XenoAcidShroudDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } action)
            return;

        args.Handled = true;
        SpawnAtPosition(ent.Comp.Spawn, ent.Owner.ToCoordinates());
        _rmcActions.ActivateSharedCooldown(action, ent);
    }
}
