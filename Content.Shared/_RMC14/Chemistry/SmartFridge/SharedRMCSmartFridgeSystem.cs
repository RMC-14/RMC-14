using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

public abstract class SharedRMCSmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

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
        _smartFridges.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _smartFridges);
        if (!_smartFridges.TryFirstOrNull(out var fridge))
            return;

        var container = _container.EnsureContainer<Container>(fridge.Value, fridge.Value.Comp.ContainerId);
        _container.Insert(transfer, container);
        Dirty(fridge.Value);
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
