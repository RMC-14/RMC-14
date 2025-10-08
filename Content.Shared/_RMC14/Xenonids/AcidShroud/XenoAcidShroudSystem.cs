using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.GasToggle;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Actions.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Xenonids.AcidShroud;

public sealed class XenoAcidShroudSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidShroudComponent, XenoAcidShroudActionEvent>(OnAcidShroudAction);
        SubscribeLocalEvent<XenoAcidShroudComponent, DoAfterAttemptEvent<XenoAcidShroudDoAfterEvent>>(OnAcidShroudDoAfterAttempt);
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

        _rmcActions.DisableSharedCooldownEvents(args.Action.Owner, ent);
    }

    private void OnAcidShroudDoAfterAttempt(Entity<XenoAcidShroudComponent> ent, ref DoAfterAttemptEvent<XenoAcidShroudDoAfterEvent> args)
    {
        if (args.Event.Target is { } action &&
            HasComp<InstantActionComponent>(action) &&
            TryComp(action, out ActionComponent? actionComp) &&
            !actionComp.Enabled)
        {
            _rmcActions.EnableSharedCooldownEvents(action, ent);
            args.Cancel();
        }
    }

    private void OnAcidShroudDoAfter(Entity<XenoAcidShroudComponent> ent, ref XenoAcidShroudDoAfterEvent args)
    {
        if (args.Target is not { } action)
            return;
        _rmcActions.EnableSharedCooldownEvents(action, ent);
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var spawn = SpawnAtPosition(ent.Comp.Spawn, ent.Owner.ToCoordinates());
        _hive.SetSameHive(ent.Owner, spawn);

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
