using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Parasite;
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
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomActionEvent>(OnXenoZoomAction);
        SubscribeLocalEvent<XenoZoomComponent, XenoZoomDoAfterEvent>(OnXenoZoomDoAfter);
        SubscribeLocalEvent<XenoZoomComponent, GetEyeOffsetEvent>(OnXenoZoomGetEyeOffset);
        SubscribeLocalEvent<XenoZoomComponent, RefreshMovementSpeedModifiersEvent>(OnXenoZoomRefreshSpeed);
        SubscribeLocalEvent<XenoZoomComponent, XenoLeapAttemptEvent>(OnLeapAttempt);
        SubscribeLocalEvent<XenoZoomComponent, XenoRestEvent>(OnRest);
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
        SetZoomEnabled(xeno, !xeno.Comp.Enabled);
    }

    private void SetZoomEnabled(Entity<XenoZoomComponent> xeno, bool enabled)
    {
        xeno.Comp.Enabled = enabled;

        if (enabled)
        {
            _contentEye.SetMaxZoom(xeno, xeno.Comp.Zoom);
            _contentEye.SetZoom(xeno, xeno.Comp.Zoom);
            xeno.Comp.Offset = Transform(xeno.Owner).LocalRotation.GetCardinalDir().ToVec() * xeno.Comp.OffsetLength;
        }
        else
        {
            _contentEye.ResetZoom(xeno);
            xeno.Comp.Offset = Vector2.Zero;
        }

        Dirty(xeno);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoZoomActionEvent>(xeno))
        {
            _actions.SetToggled((action, action), enabled);
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

    private void OnLeapAttempt(Entity<XenoZoomComponent> ent, ref XenoLeapAttemptEvent args)
    {
        if (ent.Comp.Enabled && ent.Comp.BlockLeaps)
            args.Cancelled = true;
    }

    private void OnRest(Entity<XenoZoomComponent> ent, ref XenoRestEvent args)
    {
        if (args.Resting)
        {
            ent.Comp.WasZoomedBeforeRest = ent.Comp.Enabled;

            if (!ent.Comp.Enabled)
                return;

            SetZoomEnabled(ent, false);
            return;
        }

        if (!ent.Comp.WasZoomedBeforeRest)
            return;

        ent.Comp.WasZoomedBeforeRest = false;
        SetZoomEnabled(ent, true);
    }
}
