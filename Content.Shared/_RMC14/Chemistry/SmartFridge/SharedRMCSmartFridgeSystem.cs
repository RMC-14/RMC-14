using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

public abstract class SharedRMCSmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<RMCSmartFridgeComponent>> _smartFridges = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSmartFridgeComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<RMCSmartFridgeComponent>(RMCSmartFridgeUI.Key,
            subs =>
            {
                subs.Event<RMCSmartFridgeVendMsg>(OnVend);
            });
    }

    private void OnInteractUsing(Entity<RMCSmartFridgeComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<RMCSmartFridgeInsertableComponent>(args.Used))
            return;

        Insert(ent, args.Used);
    }

    public void TransferToNearby(EntityCoordinates coords, float range, EntityUid transfer)
    {
        if (TryFindNearest(coords, range, out var fridge))
            Insert(fridge, transfer);
    }

    public void Insert(Entity<RMCSmartFridgeComponent> fridge, EntityUid item)
    {
        var container = _container.EnsureContainer<Container>(fridge, fridge.Comp.ContainerId);
        _container.Insert(item, container);
        Dirty(fridge);
    }

    public bool TryFindNearest(EntityCoordinates coords, float range, out Entity<RMCSmartFridgeComponent> fridge)
    {
        fridge = default;
        _smartFridges.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _smartFridges);

        var closest = float.MaxValue;
        foreach (var candidate in _smartFridges)
        {
            if (!coords.TryDistance(EntityManager, _transform, Transform(candidate).Coordinates, out var distance) ||
                distance >= closest)
            {
                continue;
            }

            closest = distance;
            fridge = candidate;
        }

        return fridge.Owner.Valid;
    }

    public bool TryDrainStock(Entity<RMCSmartFridgeComponent> fridge, ProtoId<ReagentPrototype> reagent, FixedPoint2 amount, out FixedPoint2 drained, IReadOnlySet<EntityUid>? exclude = null)
    {
        drained = FixedPoint2.Zero;

        if (!TryGetContainer(fridge, out var container))
            return false;

        foreach (var contained in container.ContainedEntities.ToArray())
        {
            if (drained >= amount)
                break;

            if (exclude != null && exclude.Contains(contained))
                continue;

            foreach (var (_, solnEnt) in _solution.EnumerateSolutions((contained, null)).ToArray())
            {
                var solution = solnEnt.Comp.Solution;
                var have = solution.GetReagentQuantity(new ReagentId(reagent, null));
                if (have <= FixedPoint2.Zero)
                    continue;

                var take = FixedPoint2.Min(have, amount - drained);
                if (take <= FixedPoint2.Zero)
                    continue;

                _solution.RemoveReagent(solnEnt, reagent, take);
                drained += take;

                if (solution.Volume <= FixedPoint2.Zero &&
                    TryComp(contained, out RMCSmartFridgeInsertableComponent? insertable) &&
                    insertable.Category == "Bottle")
                {
                    _container.Remove(contained, container);
                    QueueDel(contained);
                }

                if (drained >= amount)
                    break;
            }
        }

        if (drained > FixedPoint2.Zero)
            Dirty(fridge);

        return drained > FixedPoint2.Zero;
    }

    public bool TryGetEmptyContainer(
        Entity<RMCSmartFridgeComponent> fridge,
        FixedPoint2 neededVolume,
        out EntityUid container,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        EntProtoId? prototype = null)
    {
        container = default;
        solution = null;

        if (!TryGetContainer(fridge, out var fridgeContainer))
            return false;

        (EntityUid Container, Entity<SolutionComponent> Solution)? bestFit = null;
        (EntityUid Container, Entity<SolutionComponent> Solution)? bestOverall = null;
        foreach (var contained in fridgeContainer.ContainedEntities)
        {
            if (!IsEmptyContainer(contained, prototype, out var empty))
                continue;

            var maxVolume = empty.Value.Comp.Solution.MaxVolume;
            if (bestOverall == null || maxVolume > bestOverall.Value.Solution.Comp.Solution.MaxVolume)
                bestOverall = (contained, empty.Value);

            if (maxVolume >= neededVolume && (bestFit == null || maxVolume < bestFit.Value.Solution.Comp.Solution.MaxVolume))
                bestFit = (contained, empty.Value);
        }

        if ((bestFit ?? bestOverall) is not { } best)
            return false;

        container = best.Container;
        solution = best.Solution;
        return true;
    }

    public int GetEmptyContainerCount(Entity<RMCSmartFridgeComponent> fridge, EntProtoId prototype)
    {
        if (!TryGetContainer(fridge, out var container))
            return 0;

        var count = 0;
        foreach (var contained in container.ContainedEntities)
        {
            if (IsEmptyContainer(contained, prototype, out _))
                count++;
        }

        return count;
    }

    private bool IsEmptyContainer(EntityUid item, EntProtoId? prototype, [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;

        if (prototype != null && Prototype(item)?.ID != prototype.Value.Id)
            return false;

        if (!_solution.TryGetRefillableSolution(item, out var refillable, out var contents) ||
            contents.Volume > FixedPoint2.Zero ||
            contents.MaxVolume <= FixedPoint2.Zero)
        {
            return false;
        }

        solution = refillable;
        return true;
    }

    private bool TryGetContainer(Entity<RMCSmartFridgeComponent> fridge, [NotNullWhen(true)] out BaseContainer? container)
    {
        return _container.TryGetContainer(fridge, fridge.Comp.ContainerId, out container);
    }

    private void OnVend(Entity<RMCSmartFridgeComponent> ent, ref RMCSmartFridgeVendMsg args)
    {
        if (!TryGetEntity(args.Vend, out var vend))
            return;

        if (!_container.TryGetContainingContainer((vend.Value, null), out var container) ||
            container.Owner != ent.Owner ||
            container.ID != ent.Comp.ContainerId)
        {
            return;
        }

        if (_container.Remove(vend.Value, container))
            _hands.TryPickupAnyHand(args.Actor, vend.Value);
    }
}
