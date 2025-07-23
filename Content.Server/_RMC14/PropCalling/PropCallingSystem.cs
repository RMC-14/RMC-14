using Content.Server.Abilities.Mime;
using Content.Shared._RMC14.PropCalling;
using Content.Shared._RMC14.PropCalling.Events;
using Content.Shared.Actions;
using Robust.Shared.Physics;

namespace Content.Server._RMC14.PropCalling;
public sealed class PropCallingSystem : SharedPropCallingSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<PropCallingComponent>> _callersSignedUp = new();

    public override void Initialize()
    {
        base.Initialize();
        _callersSignedUp.Clear();

        SubscribeLocalEvent<PropCallingComponent, JoinOrLeaveCallingListEvent>(OnJoinOrLeaveCallingList);
        SubscribeLocalEvent<PropCallerComponent, CallOverPropsEvent>(OnCallOverProps);

        SubscribeLocalEvent<PropCallingComponent, ComponentInit>(OnPropCallingComponentInit);
        SubscribeLocalEvent<PropCallerComponent, ComponentInit>(OnPropCallerComponentInit);
    }

    public void OnPropCallingComponentInit(EntityUid uid, PropCallingComponent comp, ComponentInit args)
    {
        _actions.AddAction(uid, ref comp.TogglePropCallingEntity, comp.TogglePropCalling, uid);
    }

    public void OnPropCallerComponentInit(EntityUid uid, PropCallerComponent comp, ComponentInit args)
    {
        _actions.AddAction(uid, ref comp.CallPropsEntity, comp.CallProps, uid);
    }

    public void OnJoinOrLeaveCallingList(Entity<PropCallingComponent> ent, ref JoinOrLeaveCallingListEvent args)
    {
        if (!_callersSignedUp.TryGetValue(ent, out var listedEnt))
        {
            _callersSignedUp.Add(ent);
            return;
        }

        _callersSignedUp.Remove(listedEnt);
    }

    public void OnCallOverProps(Entity<PropCallerComponent> ent, ref CallOverPropsEvent args)
    {
        var coordinates = _transform.GetMapCoordinates(ent.Owner);

        foreach (var entity in _callersSignedUp)
        {
            _transform.SetMapCoordinates(entity.Owner, coordinates);
        }
    }
}
