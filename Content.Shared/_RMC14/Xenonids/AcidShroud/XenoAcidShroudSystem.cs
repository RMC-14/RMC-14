using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.GasToggle;
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
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoGasToggleActionEvent>(OnToggleType);
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

    private void OnToggleType(Entity<XenoAcidShroudComponent> ent, ref XenoGasToggleActionEvent args)
    {
        if (ent.Comp.Gases.Length == 0)
            return;

        var index = Array.IndexOf(ent.Comp.Gases, ent.Comp.Spawn);
        if (index == -1 || index >= ent.Comp.Gases.Length - 1)
            index = 0;
        else
            index++;

        ent.Comp.Spawn = ent.Comp.Gases[index];
        Dirty(ent);
    }
}
