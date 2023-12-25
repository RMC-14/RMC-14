using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility; using Robust.Shared.Timing;
namespace Content.Shared._CM14.Marines.Orders;

public abstract class SharedMarineOrdersSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    private readonly HashSet<Entity<MarineComponent>> _receivers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineOrdersComponent, ComponentGetStateAttemptEvent>(OnComponentGetState);
        SubscribeLocalEvent<FocusOrderComponent, ComponentGetStateAttemptEvent>(OnComponentGetState);
        SubscribeLocalEvent<HoldOrderComponent, ComponentGetStateAttemptEvent>(OnComponentGetState);
        SubscribeLocalEvent<MoveOrderComponent, ComponentGetStateAttemptEvent>(OnComponentGetState);
        SubscribeLocalEvent<ActiveOrderComponent, ComponentGetStateAttemptEvent>(OnComponentGetState);

        SubscribeLocalEvent<ActiveOrderComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<MarineOrdersComponent, EntityUnpausedEvent>(OnUnpause);

        SubscribeLocalEvent<MoveOrderComponent, ComponentShutdown>(OnMoveShutdown);

        SubscribeLocalEvent<MarineOrdersComponent, FocusActionEvent>(OnAction);
        SubscribeLocalEvent<MarineOrdersComponent, HoldActionEvent>(OnAction);
        SubscribeLocalEvent<MarineOrdersComponent, MoveActionEvent>(OnAction);
    }

    private void OnUnpause(EntityUid uid, ActiveOrderComponent comp, EntityUnpausedEvent args)
    {
        comp.Duration += args.PausedTime;
    }

    private void OnUnpause(EntityUid uid, MarineOrdersComponent comp, EntityUnpausedEvent args)
    {

        comp.Duration += args.PausedTime;
    }

    private void OnMoveShutdown(Entity<MoveOrderComponent> uid, ref ComponentShutdown ev)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
    private void OnAction(EntityUid uid, MarineOrdersComponent orders, FocusActionEvent args)
    {
        OnAction(uid, Orders.Focus, orders, args);

    }

    private void OnAction(EntityUid uid, MarineOrdersComponent orders, HoldActionEvent args)
    {
        OnAction(uid, Orders.Hold, orders, args);

    }

    private void OnAction(EntityUid uid, MarineOrdersComponent orders, MoveActionEvent args)
    {
        OnAction(uid, Orders.Move, orders, args);
    }

    private void OnAction(EntityUid uid, Orders order, MarineOrdersComponent orders, InstantActionEvent args)
    {
        if (args.Handled)
            return;

        HandleAction(uid, order, orders);

        args.Handled = true;
    }

    private void HandleAction(EntityUid uid, Orders order, MarineOrdersComponent orderComp)
    {

        if (!TryComp<TransformComponent>(uid, out var xform))
        {
            DebugTools.Assert("Order issued by an entity without TransformComponent");
            return;
        }

        // CM14 TODO Add some message for this.
        if (orderComp.Delay is not null && _timing.CurTime < orderComp.Delay)
            return;

        var active = EnsureComp<ActiveOrderComponent>(uid);
        active.Order = order;
        active.Duration = _timing.CurTime + orderComp.Duration;
        orderComp.Delay = orderComp.DefaultDelay + orderComp.Duration;

        _receivers.Clear();

        _entityLookup.GetEntitiesInRange(xform.Coordinates, orderComp.OrderRange, _receivers);

        foreach (var receiver in _receivers)
        {
            AddOrder(receiver, order, orderComp);
        }
    }

    /// <summary>
    /// Adds an order component to an entity. If the order already exists then the multiplier and duration is overriden.
    /// </summary>
    private void AddOrder(EntityUid uid, Orders order, MarineOrdersComponent orderComp)
    {
        switch (order)
        {
            case Orders.Focus:
                var focusComp = EnsureComp<FocusOrderComponent>(uid);
                focusComp.AssignMultiplier(orderComp.Multiplier);
                focusComp.Duration = _timing.CurTime + orderComp.Duration;
                break;
            case Orders.Hold:
                var holdComp = EnsureComp<HoldOrderComponent>(uid);
                holdComp.AssignMultiplier(orderComp.Multiplier);
                holdComp.Duration = _timing.CurTime + orderComp.Duration;
                break;
            case Orders.Move:
                var moveComp = EnsureComp<MoveOrderComponent>(uid);
                moveComp.AssignMultiplier(orderComp.Multiplier);
                moveComp.Duration = _timing.CurTime + orderComp.Duration;
                _movementSpeed.RefreshMovementSpeedModifiers(uid);
                break;
            default:
                DebugTools.Assert("Invalid Order");
                break;
        }
    }

    private void OnComponentGetState<T>(EntityUid uid, T comp, ComponentGetStateAttemptEvent args)
    {
        // It's null on replays apparently
        if (args.Player is null)
            return;

        args.Cancelled = HasComp<MarineComponent>(args.Player.AttachedEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        RemoveExpired<ActiveOrderComponent>();
        RemoveExpired<MoveOrderComponent>();
        RemoveExpired<FocusOrderComponent>();
        RemoveExpired<HoldOrderComponent>();
    }

    private void RemoveExpired<T>() where T: IComponent, IOrderComponent
    {
        var query = EntityQueryEnumerator<T>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.Duration)
            {
                RemCompDeferred<T>(uid);
            }
        }
    }
}
