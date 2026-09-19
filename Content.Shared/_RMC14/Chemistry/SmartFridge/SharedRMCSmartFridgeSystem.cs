using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

public abstract class SharedRMCSmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

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

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        _container.Insert(args.Used, container);
        Dirty(ent);
    }

    public void TransferToNearby(EntityCoordinates coords, float range, EntityUid transfer)
    {
        if (!TryGetNearbyFridge(coords, range, out var fridge))
            return;

        var container = _container.EnsureContainer<Container>(fridge, fridge.Comp.ContainerId);
        _container.Insert(transfer, container);
        Dirty(fridge);
    }

    public bool TryDrainStock(EntityCoordinates coords, float range, ProtoId<ReagentPrototype> reagent, FixedPoint2 amount, out FixedPoint2 drained, IReadOnlySet<EntityUid>? exclude = null)
    {
        drained = FixedPoint2.Zero;

        if (!TryGetNearbyFridgeContainer(coords, range, out var fridge, out var container))
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

    public bool TryGetEmptyContainer(EntityCoordinates coords, float range, FixedPoint2 neededVolume, out EntityUid container)
    {
        container = default;

        if (!TryGetNearbyFridgeContainer(coords, range, out _, out var fridgeContainer))
            return false;

        var bestFit = FixedPoint2.Zero;
        var bestOverall = FixedPoint2.Zero;
        EntityUid? bestFitEnt = null;
        EntityUid? bestOverallEnt = null;

        foreach (var contained in fridgeContainer.ContainedEntities)
        {
            foreach (var (_, solnEnt) in _solution.EnumerateSolutions((contained, null)))
            {
                var solution = solnEnt.Comp.Solution;
                if (solution.Volume > FixedPoint2.Zero)
                    continue;

                if (solution.MaxVolume > bestOverall)
                {
                    bestOverall = solution.MaxVolume;
                    bestOverallEnt = contained;
                }

                if (solution.MaxVolume >= neededVolume && (bestFitEnt == null || solution.MaxVolume < bestFit))
                {
                    bestFit = solution.MaxVolume;
                    bestFitEnt = contained;
                }
            }
        }

        container = bestFitEnt ?? bestOverallEnt ?? default;
        return container != default;
    }

    public bool TryGetEmptyContainerByPrototype(EntityCoordinates coords, float range, EntProtoId prototype, out EntityUid container)
    {
        container = default;

        if (!TryGetNearbyFridgeContainer(coords, range, out _, out var fridgeContainer))
            return false;

        foreach (var contained in fridgeContainer.ContainedEntities)
        {
            if (MetaData(contained).EntityPrototype?.ID != prototype.Id)
                continue;

            foreach (var (_, solnEnt) in _solution.EnumerateSolutions((contained, null)))
            {
                if (solnEnt.Comp.Solution.Volume <= FixedPoint2.Zero)
                {
                    container = contained;
                    return true;
                }

                break;
            }
        }

        return false;
    }

    public int GetEmptyContainerCount(EntityCoordinates coords, float range, EntProtoId prototype)
    {
        if (!TryGetNearbyFridgeContainer(coords, range, out _, out var fridgeContainer))
            return 0;

        var count = 0;
        foreach (var contained in fridgeContainer.ContainedEntities)
        {
            if (MetaData(contained).EntityPrototype?.ID != prototype.Id)
                continue;

            foreach (var (_, solnEnt) in _solution.EnumerateSolutions((contained, null)))
            {
                if (solnEnt.Comp.Solution.Volume <= FixedPoint2.Zero)
                    count++;

                break;
            }
        }

        return count;
    }

    private bool TryGetNearbyFridge(EntityCoordinates coords, float range, out Entity<RMCSmartFridgeComponent> fridge)
    {
        _smartFridges.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _smartFridges);

        if (_smartFridges.TryFirstOrNull(out var found))
        {
            fridge = found.Value;
            return true;
        }

        fridge = default;
        return false;
    }

    private bool TryGetNearbyFridgeContainer(EntityCoordinates coords, float range, out Entity<RMCSmartFridgeComponent> fridge, out BaseContainer container)
    {
        if (TryGetNearbyFridge(coords, range, out fridge) &&
            _container.TryGetContainer(fridge, fridge.Comp.ContainerId, out var found))
        {
            container = found;
            return true;
        }

        container = null!;
        return false;
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
