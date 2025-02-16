using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Weapons.Ranged;

public sealed class GunBatterySystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RMCGunBatterySystem _gunBattery = default!;

    private EntityQuery<GunDrainBatteryOnShootComponent> _gunDrainBatteryQuery;
    private EntityQuery<BatteryComponent> _batteryQuery;

    public override void Initialize()
    {
        _gunDrainBatteryQuery = GetEntityQuery<GunDrainBatteryOnShootComponent>();
        _batteryQuery = GetEntityQuery<BatteryComponent>();

        SubscribeLocalEvent<GunDrainBatteryOnShootComponent, MapInitEvent>(OnDrainMapInit);
        SubscribeLocalEvent<GunDrainBatteryOnShootComponent, GunShotEvent>(OnDrainBatteryShot);
        SubscribeLocalEvent<GunDrainBatteryOnShootComponent, EntInsertedIntoContainerMessage>(OnDrainInserted);
        SubscribeLocalEvent<GunDrainBatteryOnShootComponent, EntRemovedFromContainerMessage>(OnDrainRemoved);

        SubscribeLocalEvent<BatteryInGunComponent, ChargeChangedEvent>(OnBatteryInGunChargeChanged);
        SubscribeLocalEvent<BatteryInGunComponent, EntGotRemovedFromContainerMessage>(OnBatteryInGunGotRemoved);
    }

    private void OnDrainMapInit(Entity<GunDrainBatteryOnShootComponent> ent, ref MapInitEvent args)
    {
        _gunBattery.RefreshBatteryDrain((ent, ent));

        if (!_container.TryGetContainer(ent, ent.Comp.BatteryContainer, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (!_batteryQuery.HasComp(contained))
                return;

            EnsureComp<BatteryInGunComponent>(contained);
        }
    }

    private void OnDrainBatteryShot(Entity<GunDrainBatteryOnShootComponent> ent, ref GunShotEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.BatteryContainer, out var container))
            return;

        var left = ent.Comp.Drain;
        foreach (var contained in container.ContainedEntities)
        {
            if (!_batteryQuery.TryComp(contained, out var cell))
                continue;

            var change = Math.Min(left, cell.CurrentCharge);
            _battery.SetCharge(contained, cell.CurrentCharge - change, cell);

            left -= change;
            if (left <= 0)
                break;
        }

        UpdatePowered(ent, container);
    }

    private void OnDrainInserted(Entity<GunDrainBatteryOnShootComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BatteryContainer)
            return;

        UpdatePowered(ent, args.Container);
    }

    private void OnDrainRemoved(Entity<GunDrainBatteryOnShootComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BatteryContainer)
            return;

        UpdatePowered(ent, args.Container);
    }

    private void OnBatteryInGunChargeChanged(Entity<BatteryInGunComponent> ent, ref ChargeChangedEvent args)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null), out var container))
            return;

        if (!_gunDrainBatteryQuery.TryComp(container.Owner, out var drain))
            return;

        UpdatePowered((container.Owner, drain), container);
    }

    private void OnBatteryInGunGotRemoved(Entity<BatteryInGunComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RemCompDeferred<BatteryInGunComponent>(ent);
    }

    private void UpdatePowered(Entity<GunDrainBatteryOnShootComponent> gun, BaseContainer container)
    {
        foreach (var contained in container.ContainedEntities)
        {
            if (!_batteryQuery.TryComp(contained, out var battery) ||
                battery.CurrentCharge < gun.Comp.Drain)
            {
                continue;
            }

            _gunBattery.SetPowered(gun, true);
            Dirty(gun);
            return;
        }

        _gunBattery.SetPowered(gun, false);
        Dirty(gun);
    }
}
