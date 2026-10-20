using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Paratoxin.ParatoxinSlashes;

public sealed class XenoParatoxinSlashApplySystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParatoxinSlashApplyComponent, XenoParatoxinSlashActionEvent>(OnXenoParatoxinSlashEvent);
    }

    private void OnXenoParatoxinSlashEvent(Entity<XenoParatoxinSlashApplyComponent> xeno, ref XenoParatoxinSlashActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;
        var active = EnsureComp<ParatoxinCoatedSlashesComponent>(xeno);

        active.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
        active.StacksPerSlash = xeno.Comp.StackAmount;
        active.NumberOfSlashes = xeno.Comp.NumSlashes;

        Dirty(xeno, active);

        _popup.PopupClient(Loc.GetString("rmc-xeno-paratoxin-slashes-apply", ("number", xeno.Comp.NumSlashes)), xeno, xeno);
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoParatoxinSlashActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), true);
        }
    }
}
