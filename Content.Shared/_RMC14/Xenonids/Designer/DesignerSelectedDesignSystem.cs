using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed class DesignerSelectedDesignSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerSelectedDesignToggleActionEvent>(OnToggle);
    }

    private void OnToggle(Entity<DesignerStrainComponent> ent, ref DesignerSelectedDesignToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.BuildDoorNodes = !ent.Comp.BuildDoorNodes;
        _actions.SetToggled(args.Action.AsNullable(), ent.Comp.BuildDoorNodes);

        var msg = ent.Comp.BuildDoorNodes
            ? "We will now place door markers."
            : "We will now place wall markers.";
        _popup.PopupClient(msg, ent, ent, PopupType.Small);

        Dirty(ent);
    }
}
