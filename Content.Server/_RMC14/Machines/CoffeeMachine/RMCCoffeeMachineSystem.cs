using Content.Shared._RMC14.Machines.CoffeeMachine;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Machines.CoffeeMachine;

public sealed class RMCCoffeeMachineSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCoffeeMachineComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCCoffeeMachineComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCCoffeeMachineComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInteractHand(Entity<RMCCoffeeMachineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.IsBrewing)
        {
            _popup.PopupEntity(Loc.GetString("rmc-coffee-machine-brewing"), ent, args.User);
            return;
        }

        if (_timing.CurTime < ent.Comp.NextDispenseTime)
            return;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } contained)
        {
            return;
        }

        if (!_solution.TryGetRefillableSolution(contained, out var targetSolution, out var solution))
            return;

        args.Handled = true;

        if (solution.AvailableVolume <= 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-coffee-machine-full", ("container", contained)), ent, args.User);
            return;
        }

        // Start brewing
        ent.Comp.IsBrewing = true;
        ent.Comp.LastUser = args.User;
        ent.Comp.Spilled = solution.Volume > 0;
        ent.Comp.BrewFinishTime = _timing.CurTime + ent.Comp.BrewTime;

        _itemSlots.SetLock(ent, ent.Comp.SlotId, true);

        if (ent.Comp.DispenseSound != null)
            _audio.PlayPvs(ent.Comp.DispenseSound, ent);

        UpdateVisuals(ent);
        Dirty(ent);
    }

    private void FinishBrewing(Entity<RMCCoffeeMachineComponent> ent)
    {
        ent.Comp.IsBrewing = false;
        _itemSlots.SetLock(ent, ent.Comp.SlotId, false);

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } contained)
        {
            UpdateVisuals(ent);
            Dirty(ent);
            return;
        }

        if (_solution.TryGetRefillableSolution(contained, out var targetSolution, out var solution))
        {
            var amount = FixedPoint2.Min(ent.Comp.DispenseAmount, solution.AvailableVolume);
            _solution.TryAddReagent(targetSolution.Value, ent.Comp.Reagent, amount);
        }

        if (ent.Comp.Spilled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-coffee-machine-spilt"), ent, PopupType.MediumCaution);
        }

        // Handoff logic
        bool handedOff = false;
        if (ent.Comp.LastUser is { } user && TerminatingOrDeleted(user) == false)
        {
            var userTransform = Transform(user);
            var machineTransform = Transform(ent);
            if (userTransform.Coordinates.InRange(EntityManager, machineTransform.Coordinates, 2f) &&
                _hands.CountFreeHands(user) > 0)
            {
                if (_itemSlots.TryEject(ent, ent.Comp.SlotId, user, out var ejected))
                {
                    _hands.PickupOrDrop(user, ejected.Value);
                    handedOff = true;
                }
            }
        }

        if (!handedOff)
        {
            if (ent.Comp.LastUser != null)
                _popup.PopupEntity(Loc.GetString("rmc-coffee-machine-ready", ("container", contained)), ent, ent.Comp.LastUser.Value, PopupType.Medium);
        }

        ent.Comp.NextDispenseTime = _timing.CurTime + ent.Comp.Cooldown;
        UpdateVisuals(ent);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RMCCoffeeMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsBrewing)
                continue;

            if (_timing.CurTime >= comp.BrewFinishTime)
            {
                FinishBrewing((uid, comp));
            }
        }
    }

    private void OnInserted(Entity<RMCCoffeeMachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateVisuals(ent);
    }

    private void OnRemoved(Entity<RMCCoffeeMachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<RMCCoffeeMachineComponent> ent)
    {
        bool hasCup = false;
        if (_itemSlots.TryGetSlot(ent, ent.Comp.SlotId, out var slot))
        {
            hasCup = slot.ContainerSlot?.ContainedEntity != null;
        }

        _appearance.SetData(ent, RMCCoffeeMachineVisuals.HasCup, hasCup);
        _appearance.SetData(ent, RMCCoffeeMachineVisuals.IsBrewing, ent.Comp.IsBrewing);
    }
}
