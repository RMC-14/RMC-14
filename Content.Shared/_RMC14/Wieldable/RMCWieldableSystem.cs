using Content.Shared._RMC14.Wieldable.Components;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Wieldable;

public sealed class RMCWieldableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemWieldedEvent>(OnItemWielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, MapInitEvent>(OnMapInit);
    }
    
    private void OnMapInit(Entity<WieldableSpeedModifiersComponent> wieldable, ref MapInitEvent args)
    {
        wieldable.Comp.UnwieldedModifiedWalk = wieldable.Comp.UnwieldedBaseWalk;
        wieldable.Comp.UnwieldedModifiedSprint = wieldable.Comp.UnwieldedBaseSprint;
        wieldable.Comp.WieldedModifiedWalk = wieldable.Comp.WieldedBaseWalk;
        wieldable.Comp.WieldedModifiedSprint = wieldable.Comp.WieldedBaseSprint;
    }

    private void OnGotEquippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotUnequippedHandEvent args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<WieldableSpeedModifiersComponent> wieldable, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (TryComp(wieldable.Owner, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
        {
            args.Args.ModifySpeed(wieldable.Comp.WieldedModifiedWalk, wieldable.Comp.WieldedModifiedSprint);
            return;
        }
        
        args.Args.ModifySpeed(wieldable.Comp.UnwieldedModifiedWalk, wieldable.Comp.UnwieldedModifiedSprint);
    }
    
    public void RefreshSpeedModifiers(Entity<WieldableSpeedModifiersComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldableSpeedModifiersComponent>(wieldable);

        var ev = new GetWieldableSpeedModifiersEvent(
            wieldable.Comp.UnwieldedBaseWalk,
            wieldable.Comp.UnwieldedBaseSprint,
            wieldable.Comp.WieldedBaseWalk,
            wieldable.Comp.WieldedBaseSprint);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.UnwieldedModifiedWalk = ev.UnwieldedWalk > 0 ? ev.UnwieldedWalk : 0;
        wieldable.Comp.UnwieldedModifiedSprint = ev.UnwieldedSprint > 0 ? ev.UnwieldedSprint : 0;
        wieldable.Comp.WieldedModifiedWalk = ev.WieldedWalk > 0 ? ev.WieldedWalk : 0;
        wieldable.Comp.WieldedModifiedSprint = ev.WieldedSprint > 0 ? ev.WieldedSprint : 0;
        
        RefreshModifiersOnParent(wieldable.Owner);
    }

    private void OnItemUnwielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;
        
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User.Value);
    }

    private void OnItemWielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemWieldedEvent args)
    {
        RefreshModifiersOnParent(wieldable.Owner);
    }
    
    private void RefreshModifiersOnParent(EntityUid wieldableUid)
    {
        if (!TryComp(wieldableUid, out TransformComponent? transformComponent) ||
            !transformComponent.ParentUid.Valid ||
            !TryComp(transformComponent.ParentUid, out HandsComponent? handsComponent) ||
            handsComponent.ActiveHandEntity != wieldableUid)
        {
            return;
        }
        
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(transformComponent.ParentUid);
    }
}
