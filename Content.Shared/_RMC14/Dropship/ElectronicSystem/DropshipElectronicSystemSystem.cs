using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Dropship.ElectronicSystem;

public sealed class DropshipElectronicSystemSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const int MinSpread = 0;
    private const int MinBulletSpread = 1;
    private const float MinTravelTime = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipComponent, DropshipWeaponShotEvent>(OnDropshipWeaponShot);
    }

    private void OnDropshipWeaponShot(Entity<DropshipComponent> ent, ref DropshipWeaponShotEvent args)
    {
        foreach (var point in ent.Comp.AttachmentPoints)
        {
            if (!TryComp(point, out DropshipElectronicSystemPointComponent? electronic) ||
                !_container.TryGetContainer(point, electronic.ContainerId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!TryComp(contained, out DropshipTargetingSystemComponent? targeting))
                    continue;

                args.Spread = Math.Max(MinSpread, args.Spread + targeting.SpreadModifier);
                args.BulletSpread = Math.Max(MinBulletSpread, args.BulletSpread + targeting.BulletSpreadModifier);
                args.TravelTime = TimeSpan.FromSeconds(Math.Max(MinTravelTime, args.TravelTime.TotalSeconds + targeting.TravelingTimeModifier.TotalSeconds));
            }
        }
    }
}
