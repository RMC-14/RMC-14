using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.PropCalling;
using Content.Shared._RMC14.PropCalling.Events;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Configuration;

namespace Content.Server._RMC14.PropCalling;
public sealed class PropCallingSystem : SharedPropCallingSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private readonly HashSet<Entity<PropCallingComponent>> _callersSignedUp = new();
    private bool _propCallingEnabled;

    public override void Initialize()
    {
        base.Initialize();
        _callersSignedUp.Clear();

        SubscribeLocalEvent<PropCallingComponent, ToggleActionEvent>(OnJoinOrLeaveCallingList);
        SubscribeLocalEvent<PropCallerComponent, CallOverPropsEvent>(OnCallOverProps);

        SubscribeLocalEvent<PropCallingComponent, ComponentInit>(OnPropCallingComponentInit);
        SubscribeLocalEvent<PropCallerComponent, ComponentInit>(OnPropCallerComponentInit);

        SubscribeLocalEvent<PropCallingComponent, ComponentShutdown>(OnPropCallingShutdown);
        SubscribeLocalEvent<PropCallerComponent, ComponentShutdown>(OnPropCallerShutdown);

        Subs.CVar(_configuration, RMCCVars.RMCGhostPropCalling, OnGhostPropChange, true);
    }

    private void OnGhostPropChange(bool value, in CVarChangeInfo info)
    {
        _propCallingEnabled = value;

        var callingQuery = EntityQueryEnumerator<PropCallingComponent>();
        while (callingQuery.MoveNext(out var uid, out var propCalling))
        {
            if (_propCallingEnabled)
            {
                _actions.AddAction(uid, ref propCalling.TogglePropCallingEntity, propCalling.TogglePropCalling, uid);
                if (!_callersSignedUp.TryGetValue((uid, propCalling), out _))
                    _actions.SetToggled(propCalling.TogglePropCallingEntity, true);
            }
            else
                _actions.RemoveAction(uid, propCalling.TogglePropCallingEntity);
        }

        var callerQuery = EntityQueryEnumerator<PropCallerComponent>();
        while (callerQuery.MoveNext(out var uid, out var propCaller))
        {
            if (_propCallingEnabled)
                _actions.AddAction(uid, ref propCaller.CallPropsEntity, propCaller.CallProps, uid);
            else
                _actions.RemoveAction(uid, propCaller.CallPropsEntity);
        }
    }

    private void OnPropCallingComponentInit(EntityUid uid, PropCallingComponent comp, ComponentInit args)
    {
        if (_propCallingEnabled)
            _actions.AddAction(uid, ref comp.TogglePropCallingEntity, comp.TogglePropCalling, uid);
    }

    private void OnPropCallerComponentInit(EntityUid uid, PropCallerComponent comp, ComponentInit args)
    {
        if (_propCallingEnabled)
            _actions.AddAction(uid, ref comp.CallPropsEntity, comp.CallProps, uid);
    }

    private void OnPropCallingShutdown(EntityUid uid, PropCallingComponent comp, ComponentShutdown args)
    {
        var ent = (uid, comp);

        _actions.RemoveAction(uid, comp.TogglePropCallingEntity);

        if (_callersSignedUp.Contains(ent))
            _callersSignedUp.Remove(ent);
    }

    private void OnPropCallerShutdown(EntityUid uid, PropCallerComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.CallPropsEntity);
    }

    private void OnJoinOrLeaveCallingList(Entity<PropCallingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_callersSignedUp.TryGetValue(ent, out var listedEnt))
        {
            _callersSignedUp.Add(ent);
            _actions.SetToggled(ent.Comp.TogglePropCallingEntity, true);
        }
        else
        {
            _callersSignedUp.Remove(listedEnt);
            _actions.SetToggled(listedEnt.Comp.TogglePropCallingEntity, false);
        }

        args.Handled = true;
    }

    private void OnCallOverProps(Entity<PropCallerComponent> ent, ref CallOverPropsEvent args)
    {
        var coordinates = _transform.GetMapCoordinates(ent.Owner);

        foreach (var entity in _callersSignedUp)
        {
            _transform.SetMapCoordinates(entity.Owner, coordinates);
        }
    }
}
