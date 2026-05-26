using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Xenonids.Zoom;

public sealed class XenoZoomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoZoomComponent, ComponentStartup>(OnXenoZoomComponentStartup);
        SubscribeLocalEvent<XenoZoomComponent, ComponentShutdown>(OnXenoZoomComponentShutdown);
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomActionEvent>(OnXenoZoomAction);
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomDoAfterEvent>(OnXenoZoomDoAfter);
        SubscribeLocalEvent<XenoZoomComponent, GetEyeOffsetEvent>(OnXenoZoomGetEyeOffset);
        SubscribeLocalEvent<XenoZoomComponent, GetEyePvsScaleEvent>(OnXenoZoomGetEyePvsScale);
        SubscribeLocalEvent<XenoZoomComponent, RefreshMovementSpeedModifiersEvent>(OnXenoZoomRefreshSpeed);
        SubscribeLocalEvent<XenoZoomComponent, XenoLeapAttemptEvent>(OnLeapAttempt);
        SubscribeLocalEvent<XenoZoomComponent, XenoRestEvent>(OnRest);
    }

    private void OnXenoZoomComponentStartup(Entity<XenoZoomComponent> xeno, ref ComponentStartup args)
    {
        if (!TryComp<ContentEyeComponent>(xeno, out var contentEye))
            return;
        if (!TryComp<EyeComponent>(xeno, out var eye))
            return;

        _contentEye.UpdatePvsScale(xeno, contentEye, eye);
    }

    private void OnXenoZoomComponentShutdown(Entity<XenoZoomComponent> xeno, ref ComponentShutdown args)
    {
        if (!TryComp<ContentEyeComponent>(xeno, out var contentEye))
            return;
        if (!TryComp<EyeComponent>(xeno, out var eye))
            return;

        _contentEye.UpdatePvsScale(xeno, contentEye, eye);
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
            _contentEye.SetMaxZoom(xeno, xeno.Comp.Zoom);
            _contentEye.SetZoom(xeno, xeno.Comp.Zoom);
            xeno.Comp.Offset = Transform(args.User).LocalRotation.GetCardinalDir().ToVec() * xeno.Comp.OffsetLength;
        }
        else
        {
            _contentEye.SetZoom(xeno, SharedContentEyeSystem.DefaultZoom);
            xeno.Comp.Offset = Vector2.Zero;
        }

        Dirty(xeno);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoZoomActionEvent>(xeno))
        {
            _actions.SetToggled((action, action), xeno.Comp.Enabled);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

        if (TryComp(xeno, out EyeComponent? eye))
            _contentEye.UpdateEyeOffset((xeno.Owner, eye));
    }

    private void OnXenoZoomGetEyeOffset(Entity<XenoZoomComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (ent.Comp.Running)
            args.Offset += ent.Comp.Offset;
    }

    private void OnXenoZoomGetEyePvsScale(Entity<XenoZoomComponent> ent, ref GetEyePvsScaleEvent args)
    {
        // We try to scale up the PVS radius while leaving the same amount of tiles as overhead.
        // pvsOverheadEstimate estimates what portion of the viewport is added on as "overhead" for PVS purposes.
        // i.e. an estimate of 0.2 means the PVS radius is 20% larger than the actual viewport radius.
        // A smaller estimate results in a LARGER pvs radius scale up, because there's less overhead proportionally.
        // TODO RMC14 calculate the overhead from CVars instead of hardcoding it here. I calculated the default viewport
        // as 21 tiles wide while the PVS bound appears to be 25 tiles wide. That's about 20% overhead.
        if (ent.Comp.Running)
        {
            const float pvsOverheadEstimate = 0.2f;
            var scale = (Math.Max(ent.Comp.Zoom.X, ent.Comp.Zoom.Y) + pvsOverheadEstimate) / (1 + pvsOverheadEstimate);
            // args.Scale is added to the scale, not multiplied, so we have to subtract 1 and make sure we don't scale down.
            args.Scale += Math.Max(0, scale - 1);
        }
    }

    private void OnXenoZoomRefreshSpeed(Entity<XenoZoomComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Enabled)
            args.ModifySpeed(ent.Comp.Speed, ent.Comp.Speed);
    }

    private void OnLeapAttempt(Entity<XenoZoomComponent> ent, ref XenoLeapAttemptEvent args)
    {
        if (ent.Comp.Enabled && ent.Comp.BlockLeaps)
            args.Cancelled = true;
    }

    private void OnRest(Entity<XenoZoomComponent> ent, ref XenoRestEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        ent.Comp.Enabled = false;
        _contentEye.ResetZoom(ent);
        ent.Comp.Offset = Vector2.Zero;
        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (TryComp(ent, out EyeComponent? eye))
            _contentEye.UpdateEyeOffset((ent.Owner, eye));

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoZoomActionEvent>(ent))
        {
            _actions.SetToggled((action, action), ent.Comp.Enabled);
        }
    }
}
