using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

public sealed class SmartGunSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SmartGunBatteryComponent, ContainerGettingInsertedAttemptEvent>(OnBatteryInsertedAttempt);
    }

    private void OnBatteryInsertedAttempt(Entity<SmartGunBatteryComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
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
