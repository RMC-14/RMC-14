using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleViewToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleViewToggleComponent, RMCVehicleToggleViewActionEvent>(OnToggleViewAction);
        SubscribeLocalEvent<RMCVehicleViewToggleComponent, ComponentShutdown>(OnToggleViewShutdown);
    }

    public void EnableViewToggle(
        EntityUid user,
        EntityUid outsideTarget,
        EntityUid source,
        EntityUid? insideTarget,
        bool isOutside)
    {
        var toggle = EnsureComp<RMCVehicleViewToggleComponent>(user);
        toggle.Source = source;
        toggle.OutsideTarget = outsideTarget;
        toggle.InsideTarget = insideTarget;
        toggle.IsOutside = isOutside;

        if (toggle.Action == null)
            toggle.Action = _actions.AddAction(user, toggle.ActionId);

        if (toggle.Action is { } actionUid && TryComp(actionUid, out ActionComponent? actionComp))
        {
            actionComp.ItemIconStyle = ItemActionIconStyle.BigAction;
            actionComp.EntIcon = null;
            Dirty(actionUid, actionComp);
        }

        UpdateActionState(toggle);
        Dirty(user, toggle);
    }

    public void DisableViewToggle(EntityUid user, EntityUid source)
    {
        if (!TryComp(user, out RMCVehicleViewToggleComponent? toggle))
            return;

        if (toggle.Source != source)
            return;

        if (toggle.Action is { } action &&
            TryComp(action, out ActionComponent? actionComp) &&
            actionComp.AttachedEntity == user)
        {
            _actions.RemoveAction(user, action);
        }

        toggle.Action = null;

        RemCompDeferred<RMCVehicleViewToggleComponent>(user);
    }

    private void OnToggleViewShutdown(Entity<RMCVehicleViewToggleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Action is not { } action)
            return;

        if (TryComp(action, out ActionComponent? actionComp) &&
            actionComp.AttachedEntity == ent.Owner)
        {
            _actions.RemoveAction(ent.Owner, action);
        }
    }

    private void OnToggleViewAction(Entity<RMCVehicleViewToggleComponent> ent, ref RMCVehicleToggleViewActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;

        if (ent.Comp.OutsideTarget == null || !TryComp(ent.Owner, out EyeComponent? eye))
            return;

        var outside = ent.Comp.OutsideTarget.Value;
        if (eye.Target == outside)
        {
            _eye.SetTarget(ent.Owner, ent.Comp.InsideTarget, eye);
            ent.Comp.IsOutside = false;
        }
        else
        {
            ent.Comp.InsideTarget = eye.Target;
            _eye.SetTarget(ent.Owner, outside, eye);
            ent.Comp.IsOutside = true;
        }

        UpdateActionState(ent.Comp);
        Dirty(ent.Owner, ent.Comp);
    }

    private void UpdateActionState(RMCVehicleViewToggleComponent toggle)
    {
        if (toggle.Action == null)
            return;

        _actions.SetToggled(toggle.Action, toggle.IsOutside);
    }
}
