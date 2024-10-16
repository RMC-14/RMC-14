using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry;

public sealed class RMCChemistrySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<Entity<RMCChemicalDispenserComponent>> _dispensers = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChemicalDispenserComponent, MapInitEvent>(OnDispenserMapInit);

        Subs.BuiEvents<RMCChemicalDispenserComponent>(RMCChemicalDispenserUi.Key,
            subs =>
            {
                subs.Event<RMCChemicalDispenserDispenseSettingBuiMsg>(OnChemicalDispenserSettingMsg);
                subs.Event<RMCChemicalDispenserBeakerSettingBuiMsg>(OnChemicalDispenserBeakerSettingMsg);
                subs.Event<RMCChemicalDispenserEjectBeakerBuiMsg>(OnChemicalDispenserEjectBeakerMsg);
                subs.Event<RMCChemicalDispenserDispenseBuiMsg>(OnChemicalDispenserDispenseMsg);
            });
    }

    private void OnDispenserMapInit(Entity<RMCChemicalDispenserComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerSlotId);
    }

    private void OnChemicalDispenserSettingMsg(Entity<RMCChemicalDispenserComponent> ent, ref RMCChemicalDispenserDispenseSettingBuiMsg args)
    {
        if (!ent.Comp.Settings.Contains(args.Amount))
            return;

        ent.Comp.DispenseSetting = args.Amount;
        Dirty(ent);
    }

    private void OnChemicalDispenserBeakerSettingMsg(Entity<RMCChemicalDispenserComponent> ent, ref RMCChemicalDispenserBeakerSettingBuiMsg args)
    {
        if (!ent.Comp.Settings.Contains(args.Amount))
            return;

        ent.Comp.BeakerSetting = args.Amount;
        Dirty(ent);
    }

    private void OnChemicalDispenserEjectBeakerMsg(Entity<RMCChemicalDispenserComponent> ent, ref RMCChemicalDispenserEjectBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.ContainerSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor);
        Dirty(ent);
    }

    private void OnChemicalDispenserDispenseMsg(Entity<RMCChemicalDispenserComponent> ent, ref RMCChemicalDispenserDispenseBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.ContainerSlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } contained ||
            !_solution.TryGetMixableSolution(contained, out var solutionEnt, out _) ||
            !ent.Comp.Reagents.Contains(args.Reagent) ||
            !TryGetStorage(ent.Comp.Network, out var storage))
        {
            return;
        }

        var cost = ent.Comp.CostPerUnit * ent.Comp.DispenseSetting;
        if (cost > storage.Comp.Energy)
            return;

        ChangeStorageEnergy(storage, storage.Comp.Energy - cost);
        _solution.TryAddReagent(solutionEnt.Value, args.Reagent, ent.Comp.DispenseSetting);
    }

    public bool TryGetStorage(EntProtoId network, out Entity<RMCChemicalStorageComponent> storage)
    {
        var storages = EntityQueryEnumerator<RMCChemicalStorageComponent>();
        while (storages.MoveNext(out var storageId, out var storageComp))
        {
            if (storageComp.Network == network)
            {
                storage = (storageId, storageComp);
                return true;
            }
        }

        storage = default;
        return false;
    }

    public void ChangeStorageEnergy(Entity<RMCChemicalStorageComponent> storage, FixedPoint2 energy)
    {
        storage.Comp.Energy = FixedPoint2.Clamp(energy, FixedPoint2.Zero, storage.Comp.MaxEnergy);
        Dirty(storage);

        var dispensers = EntityQueryEnumerator<RMCChemicalDispenserComponent>();
        while (dispensers.MoveNext(out var dispenserId, out var dispenserComp))
        {
            dispenserComp.Energy = energy;
            Dirty(dispenserId, dispenserComp);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var storages = EntityQueryEnumerator<RMCChemicalStorageComponent>();
        while (storages.MoveNext(out var storageId, out var storage))
        {
            if (time < storage.RechargeAt)
                continue;

            storage.RechargeAt = time + storage.RechargeEvery;
            Dirty(storageId, storage);

            _dispensers.Clear();
            var dispensers = EntityQueryEnumerator<RMCChemicalDispenserComponent>();
            while (dispensers.MoveNext(out var dispenserId, out var dispenser))
            {
                if (dispenser.Network == storage.Network)
                    _dispensers.Add((dispenserId, dispenser));
            }

            storage.MaxEnergy = storage.BaseMax + storage.MaxPer * _dispensers.Count;
            storage.Recharge = storage.BaseRecharge + storage.RechargePer * _dispensers.Count;

            if (!storage.Updated)
            {
                storage.Updated = true;
                storage.Energy = storage.MaxEnergy;
            }
            else
            {
                storage.Energy = FixedPoint2.Min(storage.MaxEnergy, storage.Energy + storage.Recharge);
            }

            foreach (var dispenser in _dispensers)
            {
                dispenser.Comp.Energy = storage.Energy;
                dispenser.Comp.MaxEnergy = storage.MaxEnergy;
                Dirty(dispenser);
            }
        }
    }
}
