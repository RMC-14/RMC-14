using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.Eye;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public sealed class XenoParasiteVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<XenoParasiteVisionComponent, ActionXenoParasiteVisionEvent>(OnToggleVision);
        SubscribeLocalEvent<XenoParasiteVisionComponent, XenoLeapAttemptEvent>(OnLeapAttempt);
        SubscribeLocalEvent<XenoParasiteVisionComponent, XenoRestEvent>(OnRest);
        SubscribeLocalEvent<XenoParasiteVisionComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeLocalEvent<XenoParasiteVisionComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnToggleVision(Entity<XenoParasiteVisionComponent> ent, ref ActionXenoParasiteVisionEvent args)
    {
        ent.Comp.Enabled = !ent.Comp.Enabled;

        if (ent.Comp.Enabled)
        {
            _contentEye.SetMaxZoom(ent, ent.Comp.Zoom);
            _contentEye.SetZoom(ent, ent.Comp.Zoom);
            ent.Comp.Offset = Transform(ent).LocalRotation.GetCardinalDir().ToVec() * ent.Comp.OffsetLength;
        }
        else
        {
            _contentEye.ResetZoom(ent);
            ent.Comp.Offset = Vector2.Zero;
        }

        Dirty(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<ActionXenoParasiteVisionEvent>(ent))
        {
            _actions.SetToggled((action, action), ent.Comp.Enabled);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (TryComp(ent, out EyeComponent? eye))
            _contentEye.UpdateEyeOffset((ent.Owner, eye));

        args.Handled = true;
    }

    private void OnGetEyeOffset(Entity<XenoParasiteVisionComponent> ent, ref GetEyeOffsetEvent args)
    {
        args.Offset += ent.Comp.Offset;
    }

    private void OnRefreshSpeed(Entity<XenoParasiteVisionComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Enabled)
            args.ModifySpeed(ent.Comp.Speed, ent.Comp.Speed);
    }

    private void OnLeapAttempt(Entity<XenoParasiteVisionComponent> ent, ref XenoLeapAttemptEvent args)
    {
        if (ent.Comp.Enabled)
            args.Cancelled = true;
    }

    private void OnRest(Entity<XenoParasiteVisionComponent> ent, ref XenoRestEvent args)
    {
        if (ent.Comp.Enabled)
        {
            ent.Comp.Enabled = false;
            _contentEye.ResetZoom(ent);
            ent.Comp.Offset = Vector2.Zero;
            Dirty(ent);
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
            
            if (TryComp(ent, out EyeComponent? eye))
                _contentEye.UpdateEyeOffset((ent.Owner, eye));
                
            foreach (var action in _rmcActions.GetActionsWithEvent<ActionXenoParasiteVisionEvent>(ent))
            {
                _actions.SetToggled((action, action), ent.Comp.Enabled);
            }
        }
    }
}