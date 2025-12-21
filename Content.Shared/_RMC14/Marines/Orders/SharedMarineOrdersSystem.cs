using Content.Shared._RMC14.Evasion;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.Orders;

public abstract class SharedMarineOrdersSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EvasionSystem _evasionSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<MarineComponent>> _receivers = new();

    private EntityQuery<MoveOrderArmorComponent> _moveOrderArmorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _moveOrderArmorQuery = GetEntityQuery<MoveOrderArmorComponent>();

        SubscribeLocalEvent<MoveOrderComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<FocusOrderComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<HoldOrderComponent, EntityUnpausedEvent>(OnUnpause);

        SubscribeLocalEvent<MarineOrdersComponent, FocusActionEvent>(OnAction);
        SubscribeLocalEvent<MarineOrdersComponent, HoldActionEvent>(OnAction);
        SubscribeLocalEvent<MarineOrdersComponent, MoveActionEvent>(OnAction);

        SubscribeLocalEvent<MoveOrderComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
        SubscribeLocalEvent<MoveOrderComponent, ComponentShutdown>(OnMoveShutdown);
        SubscribeLocalEvent<MoveOrderComponent, EvasionRefreshModifiersEvent>(OnMoveOrderEvasionRefresh);
        SubscribeLocalEvent<MoveOrderComponent, DidEquipEvent>(OnMoveOrderDidEquip);
        SubscribeLocalEvent<MoveOrderComponent, DidUnequipEvent>(OnMoveOrderDidUnequip);

        SubscribeLocalEvent<HoldOrderComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<HoldOrderComponent> orders, ref DamageModifyEvent args)
    {
        var comp = orders.Comp;
        if (comp.Received.Count == 0)
            return;

        var damage = args.Damage.DamageDict;
        var multiplier = 1 - comp.DamageModifier * TransformOrderLevel(comp.Received[0].Multiplier);

        foreach (var type in comp.DamageTypes)
        {
            if (damage.TryGetValue(type, out var amount))
                damage[type] = amount * multiplier;
        }
    }

    private void OnUnpause<T>(Entity<T> orders, ref EntityUnpausedEvent args) where T : IComponent, IOrderComponent
    {
        var comp = orders.Comp;
        for (var i = 0; i < comp.Received.Count; i++)
        {
            var received = comp.Received[i];
            comp.Received[i] = (received.Multiplier, received.ExpiresAt + args.PausedTime);
        }
    }

    private void OnRefreshMovement(Entity<MoveOrderComponent> orders, ref RefreshMovementSpeedModifiersEvent args)
    {
        var comp = orders.Comp;
        if (comp.Received.Count == 0)
            return;

        var hasArmor = false;
        var armorEnumerator = _inventory.GetSlotEnumerator(orders.Owner, SlotFlags.OUTERCLOTHING);
        while (armorEnumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity == null)
                continue;

            if (_moveOrderArmorQuery.HasComp(slot.ContainedEntity))
            {
                hasArmor = true;
                break;
            }
        }

        if (!hasArmor)
            return;

        var speed = 1 + (comp.MoveSpeedModifier * TransformOrderLevel(comp.Received[0].Multiplier)).Float();
        args.ModifySpeed(speed, speed);
    }

    private void OnMoveShutdown(Entity<MoveOrderComponent> uid, ref ComponentShutdown ev)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        _evasionSystem.RefreshEvasionModifiers(uid.Owner);
    }

    private void OnMoveOrderEvasionRefresh(Entity<MoveOrderComponent> entity, ref EvasionRefreshModifiersEvent args)
    {
        if (entity.Owner != args.Entity.Owner)
            return;

        if (entity.Comp.Received.Count == 0)
            return;

        args.Evasion += TransformOrderLevel(entity.Comp.Received[0].Multiplier) * entity.Comp.EvasionModifier;
    }

    private void OnMoveOrderDidEquip(Entity<MoveOrderComponent> ent, ref DidEquipEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMoveOrderDidUnequip(Entity<MoveOrderComponent> ent, ref DidUnequipEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    protected virtual void OnAction(Entity<MarineOrdersComponent> orders, ref FocusActionEvent args)
    {
        OnAction<FocusOrderComponent>(orders, args);
    }

    protected virtual void OnAction(Entity<MarineOrdersComponent> orders, ref HoldActionEvent args)
    {
        OnAction<HoldOrderComponent>(orders, args);
    }

    protected virtual void OnAction(Entity<MarineOrdersComponent> orders, ref MoveActionEvent args)
    {
        OnAction<MoveOrderComponent>(orders, args);
    }

    private void OnAction<T>(Entity<MarineOrdersComponent> orders, InstantActionEvent args) where T : IOrderComponent, new()
    {
        if (args.Handled)
            return;

        if (HandleAction<T>(orders))
            args.Handled = true;
    }

    private bool HandleAction<T>(Entity<MarineOrdersComponent> orders) where T : IOrderComponent, new()
    {
        if (!TryComp(orders, out TransformComponent? xform) ||
            _mobState.IsDead(orders))
        {
            return false;
        }

        var level = Math.Max(1, _skills.GetSkill(orders.Owner, orders.Comp.Skill));
        var duration = orders.Comp.Duration * (TransformOrderLevel(level) + 1);

        _actions.SetCooldown(orders.Comp.FocusActionEntity, orders.Comp.Cooldown);
        _actions.SetCooldown(orders.Comp.MoveActionEntity, orders.Comp.Cooldown);
        _actions.SetCooldown(orders.Comp.HoldActionEntity, orders.Comp.Cooldown);

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, orders.Comp.OrderRange, _receivers);

        foreach (var receiver in _receivers)
        {
            if (_mobState.IsDead(receiver))
                continue;

            AddOrder<T>(receiver, level, duration);
        }

        return true;
    }

    /// <summary>
    /// Adds an order component to an entity. If the order already exists then the multiplier and duration is overriden.
    /// </summary>
    private void AddOrder<T>(Entity<MarineComponent> receiver, float multiplier, TimeSpan duration) where T : IOrderComponent, new()
    {
        var time = _timing.CurTime;
        var comp = EnsureComp<T>(receiver);

        comp.Received.Add((multiplier, time + duration));
        comp.Received.Sort((a, b) => a.CompareTo(b));

        _movementSpeed.RefreshMovementSpeedModifiers(receiver);
        _evasionSystem.RefreshEvasionModifiers(receiver);
    }

    private void RemoveExpired<T>() where T : IComponent, IOrderComponent
    {
        var query = EntityQueryEnumerator<T>();
        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            for (var i = comp.Received.Count - 1; i >= 0; i--)
            {
                var received = comp.Received[i];
                if (received.ExpiresAt < time)
                    comp.Received.RemoveAt(i);
            }

            if (comp.Received.Count == 0)
                RemCompDeferred<T>(uid);
        }
    }

    public void StartActionUseDelay(Entity<MarineOrdersComponent> orders)
    {
        _actions.StartUseDelay(orders.Comp.HoldActionEntity);
        _actions.StartUseDelay(orders.Comp.MoveActionEntity);
        _actions.StartUseDelay(orders.Comp.FocusActionEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        RemoveExpired<MoveOrderComponent>();
        RemoveExpired<FocusOrderComponent>();
        RemoveExpired<HoldOrderComponent>();
    }

    /// <summary>
    /// Translates the order level to the actual effective multiplier we use.
    /// </summary>
    private static float TransformOrderLevel(FixedPoint2 value)
    {
        var v = value.Float();
        return v switch
        {
            1f => 1f,
            2f => 1.5f,
            3f => 2f,
            4f => 3f,
            _ => v,
        };
    }

    private static float TransformOrderLevel(int value)
    {
        return value switch
        {
            1 => 1f,
            2 => 1.5f,
            3 => 2f,
            4 => 3f,
            _ => value,
        };
    }
}
