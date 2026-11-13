using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.PowerCell.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

public sealed class SmartGunSystem : EntitySystem
{
    [Dependency] private readonly RMCGunBatterySystem _gunBattery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SmartGunBatteryComponent, ContainerGettingInsertedAttemptEvent>(OnBatteryInsertedAttempt);
        SubscribeLocalEvent<SmartGunComponent, GunGetBatteryDrainEvent>(OnGetBatteryDrain);
        SubscribeLocalEvent<SmartGunComponent, ToggleActionEvent>(OnToggleIFF, after: [typeof(IFFToggleSystem)]);
    }

    private void OnGetBatteryDrain(Entity<SmartGunComponent> ent, ref GunGetBatteryDrainEvent args)
    {
        if (TryComp(ent, out GunIFFComponent? iff) && !iff.Enabled)
            args.Drain -= ent.Comp.IFFBatteryDrain;
    }

    private void OnToggleIFF(Entity<SmartGunComponent> ent, ref ToggleActionEvent args)
    {
        if (TryComp(ent, out IFFToggleComponent? iff) && args.Action == iff.Action)
            _gunBattery.RefreshBatteryDrain(ent.Owner);
    }

    private void OnBatteryInsertedAttempt(Entity<SmartGunBatteryComponent> ent,
        ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var container = args.Container;
        if (TryComp(container.Owner, out PowerCellSlotComponent? slot) &&
            container.ID == slot.CellSlotId &&
            !HasComp<SmartGunComponent>(container.Owner))
        {
            args.Cancel();
        }
    }
}
