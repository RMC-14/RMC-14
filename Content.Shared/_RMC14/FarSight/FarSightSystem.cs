using Content.Shared.Actions;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.FarSight;

public sealed class FarSightSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FarSightItemComponent, GetItemActionsEvent>(OnFarSightGetItemActions);
        SubscribeLocalEvent<FarSightItemComponent, FarSightActionEvent>(OnFarSightAction);
    }

    private void OnFarSightGetItemActions(Entity<FarSightItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnFarSightAction(Entity<FarSightItemComponent> ent, ref FarSightActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        var user = args.Performer;
        if (ent.Comp.Enabled)
        {
            _eye.SetMaxZoom(user, ent.Comp.Zoom);
            _eye.SetZoom(user, ent.Comp.Zoom);
        }
        else
        {
            _eye.ResetZoom(user);
        }

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Enabled);
        _appearance.SetData(ent, FarSightItemVisuals.Active, ent.Comp.Enabled);
    }
}
