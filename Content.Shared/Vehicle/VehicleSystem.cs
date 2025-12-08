using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    public override void Initialize()
    {
        InitializeOperator();
        InitializeKey();

        SubscribeLocalEvent<VehicleComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<VehicleComponent, UpdateCanMoveEvent>(OnVehicleUpdateCanMove);
        SubscribeLocalEvent<VehicleComponent, ComponentShutdown>(OnVehicleShutdown);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnVehicleGetAdditionalAccess);

        SubscribeLocalEvent<VehicleOperatorComponent, ComponentShutdown>(OnOperatorShutdown);
    }

    private void OnBeforeDamageChanged(Entity<VehicleComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.TransferDamage || !args.Damage.AnyPositive() || ent.Comp.Operator is not { } operatorUid)
            return;

        var damage = DamageSpecifier.GetPositive(args.Damage);

        if (ent.Comp.TransferDamageModifier is { } modifierSet)
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);

        _damageable.TryChangeDamage(operatorUid, damage, origin: args.Origin);
    }

    private void OnVehicleUpdateCanMove(Entity<VehicleComponent> ent, ref UpdateCanMoveEvent args)
    {
        var ev = new VehicleCanRunEvent(ent);
        RaiseLocalEvent(ent, ref ev);
        if (!ev.CanRun)
            args.Cancel();
    }

    private void OnVehicleShutdown(Entity<VehicleComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator(ent);
    }

    private void OnVehicleGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Operator is { } operatorUid)
            args.Entities.Add(operatorUid);
    }

    private void OnOperatorShutdown(Entity<VehicleOperatorComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator((ent, ent));
    }

    public bool TrySetOperator(Entity<VehicleComponent> entity, EntityUid? uid, bool removeExisting = true)
    {
        if (entity.Comp.Operator == null && uid is null)
            return false;

        if (TryComp<VehicleOperatorComponent>(uid, out var eOperator))
            return eOperator.Vehicle == entity.Owner;

        if (!removeExisting && entity.Comp.Operator is not null)
            return false;

        if (uid != null && !CanOperate(entity.AsNullable(), uid.Value))
            return false;

        var oldOperator = entity.Comp.Operator;

        if (entity.Comp.Operator is { } currentOperator && TryComp<VehicleOperatorComponent>(currentOperator, out var currentOperatorComponent))
        {
            var exitEvent = new OnVehicleExitedEvent(entity, currentOperator);
            RaiseLocalEvent(currentOperator, ref exitEvent);

            currentOperatorComponent.Vehicle = null;
            RemCompDeferred<VehicleOperatorComponent>(currentOperator);
            RemCompDeferred<RelayInputMoverComponent>(currentOperator);
            RemCompDeferred<GridVehicleOperatorComponent>(currentOperator);
        }

        entity.Comp.Operator = uid;

        if (uid != null)
        {
            var vehicleOperator = AddComp<VehicleOperatorComponent>(uid.Value);
            vehicleOperator.Vehicle = entity.Owner;
            Dirty(uid.Value, vehicleOperator);

            if (entity.Comp.MovementKind == VehicleMovementKind.Standard)
            {
                _mover.SetRelay(uid.Value, entity);
            }
            else if (entity.Comp.MovementKind == VehicleMovementKind.Grid)
            {
                EnsureComp<GridVehicleMoverComponent>(entity.Owner);
                EnsureComp<GridVehicleOperatorComponent>(uid.Value);
                RemCompDeferred<RelayInputMoverComponent>(uid.Value);
                RemCompDeferred<MovementRelayTargetComponent>(entity);
            }

            var enterEvent = new OnVehicleEnteredEvent(entity, uid.Value);
            RaiseLocalEvent(uid.Value, ref enterEvent);
        }
        else
        {
            RemCompDeferred<MovementRelayTargetComponent>(entity);
        }

        RefreshCanRun((entity, entity.Comp));

        var setEvent = new VehicleOperatorSetEvent(uid, oldOperator);
        RaiseLocalEvent(entity, ref setEvent);

        Dirty(entity);
        return true;
    }

    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleComponent> entity)
    {
        return TrySetOperator(entity, null, removeExisting: true);
    }

    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleOperatorComponent?> operatorEntity)
    {
        if (!Resolve(operatorEntity, ref operatorEntity.Comp, false))
            return true;

        if (!TryComp<VehicleComponent>(operatorEntity.Comp.Vehicle, out var vehicle))
            return true;

        return TrySetOperator((operatorEntity.Comp.Vehicle.Value, vehicle), null, removeExisting: true);
    }

    [PublicAPI]
    public bool TryGetOperator(Entity<VehicleComponent?> entity, [NotNullWhen(true)] out Entity<VehicleOperatorComponent>? operatorEnt)
    {
        operatorEnt = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (entity.Comp.Operator is not { } operatorUid)
            return false;

        if (!TryComp<VehicleOperatorComponent>(operatorUid, out var operatorComponent))
            return false;

        operatorEnt = (operatorUid, operatorComponent);
        return true;
    }

    public EntityUid? GetOperatorOrNull(Entity<VehicleComponent?> entity)
    {
        TryGetOperator(entity, out var operatorEnt);
        return operatorEnt;
    }

    [PublicAPI]
    public bool HasOperator(Entity<VehicleComponent?> entity)
    {
        return TryGetOperator(entity, out _);
    }

    public bool CanOperate(Entity<VehicleComponent?> entity, EntityUid uid)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (_entityWhitelist.IsWhitelistFail(entity.Comp.OperatorWhitelist, uid))
            return false;

        return _actionBlocker.CanConsciouslyPerformAction(uid);
    }

    public void RefreshCanRun(Entity<VehicleComponent?> entity)
    {
        if (TerminatingOrDeleted(entity))
            return;

        if (!Resolve(entity, ref entity.Comp))
            return;

        _actionBlocker.UpdateCanMove(entity);
        UpdateAppearance((entity, entity.Comp));
    }

    private void UpdateAppearance(Entity<VehicleComponent> entity)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        if (TryComp<InputMoverComponent>(entity, out var inputMover))
            _appearance.SetData(entity, VehicleVisuals.CanRun, inputMover.CanMove, appearance);

        _appearance.SetData(entity, VehicleVisuals.HasOperator, entity.Comp.Operator is not null, appearance);
    }
}
