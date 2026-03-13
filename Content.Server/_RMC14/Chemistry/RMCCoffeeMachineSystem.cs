using Content.Shared._RMC14.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chemistry;

public sealed class RMCCoffeeMachineSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCoffeeMachineComponent, InteractHandEvent>(OnInteractHand);
    }


    private void OnInteractHand(Entity<RMCCoffeeMachineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

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

        var amount = FixedPoint2.Min(ent.Comp.DispenseAmount, solution.AvailableVolume);
        _solution.TryAddReagent(targetSolution.Value, ent.Comp.Reagent, amount);

        if (ent.Comp.DispenseSound != null)
            _audio.PlayPvs(ent.Comp.DispenseSound, ent);

        _popup.PopupEntity(Loc.GetString("rmc-coffee-machine-dispense", ("container", contained)), ent, args.User);

        ent.Comp.NextDispenseTime = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);
    }
}
