using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Rest;
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
        _contentEye.UpdatePvsScale(xeno);
    }

    private void OnXenoZoomComponentShutdown(Entity<XenoZoomComponent> xeno, ref ComponentShutdown args)
    {
        _contentEye.UpdatePvsScale(xeno);
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
        // Scale is additive, i.e. adding .2 would increase PVS by 20%, so we subtract 1 from the zoom levels first.
        // TODO RMC14 For now we divide by 2 in order to reduce how much "extra" overhead PVS we have outside of viewport range,
        // because scale also scales up the default overhead amount, and we don't actually want the overhead scaled.
        // Dividing by 2 only works for lower zoom levels (under 2). If we get higher zoom levels, come back to this and fix it.
        if (ent.Comp.Running)
            args.Scale += Math.Max(0, Math.Max(ent.Comp.Zoom.X - 1, ent.Comp.Zoom.Y - 1)) / 2;
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
