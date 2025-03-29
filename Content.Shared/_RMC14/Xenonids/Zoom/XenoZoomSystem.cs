using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Xenonids.Zoom;

public sealed class XenoZoomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomActionEvent>(OnXenoZoomAction);
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomDoAfterEvent>(OnXenoZoomDoAfter);
        SubscribeLocalEvent<XenoZoomComponent, GetEyeOffsetEvent>(OnXenoZoomGetEyeOffset);
        SubscribeLocalEvent<XenoZoomComponent, RefreshMovementSpeedModifiersEvent>(OnXenoZoomRefreshSpeed);
    }

    private void OnXenoZoomAction(Entity<XenoZoomComponent> xeno, ref XenoZoomActionEvent args)
    {
        var ev = new XenoZoomDoAfterEvent();
        var delay = xeno.Comp.Enabled ? TimeSpan.Zero : xeno.Comp.DoAfter;
        var doAfter = new DoAfterArgs(EntityManager, xeno, delay, ev, xeno) { BreakOnMove = true };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoZoomDoAfter(Entity<XenoZoomComponent> xeno, ref XenoZoomDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        xeno.Comp.Enabled = !xeno.Comp.Enabled;

        if (xeno.Comp.Enabled)
        {
            _eye.SetMaxZoom(xeno, xeno.Comp.Zoom);
            _eye.SetZoom(xeno, xeno.Comp.Zoom);
            xeno.Comp.Offset = Transform(args.User).LocalRotation.GetCardinalDir().ToVec() * xeno.Comp.OffsetLength;
        }
        else
        {
            _eye.ResetZoom(xeno);
            xeno.Comp.Offset = Vector2.Zero;
        }

        Dirty(xeno);

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoZoomActionEvent)
                _actions.SetToggled(actionId, xeno.Comp.Enabled);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

        if (TryComp(xeno, out EyeComponent? eye))
            _contentEye.UpdateEyeOffset((xeno.Owner, eye));
    }

    private void OnXenoZoomGetEyeOffset(Entity<XenoZoomComponent> ent, ref GetEyeOffsetEvent args)
    {
        args.Offset += ent.Comp.Offset;
    }

    private void OnXenoZoomRefreshSpeed(Entity<XenoZoomComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Enabled)
            args.ModifySpeed(ent.Comp.Speed, ent.Comp.Speed);
    }
}
