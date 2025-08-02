using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class ShootUseDelaySystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private const string ShootUseDelayId = "CMShootUseDelay";

    public override void Initialize()
    {
        SubscribeLocalEvent<ShootUseDelayComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<ShootUseDelayComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp(ent, out UseDelayComponent? delayComponent) || !TryComp(ent, out GunComponent? gunComponent))
            return;

        _useDelay.SetLength((ent.Owner, delayComponent), TimeSpan.FromSeconds(1f / gunComponent.FireRateModified), ShootUseDelayId);
        _useDelay.TryResetDelay((ent.Owner, delayComponent), true, id: ShootUseDelayId);
    }
}
