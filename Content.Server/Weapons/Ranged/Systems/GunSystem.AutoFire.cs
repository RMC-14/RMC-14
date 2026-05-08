using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        /*
         * On server because client doesn't want to predict other's guns.
         */

        // Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled
        var query = EntityQueryEnumerator<GunComponent>();

        while (query.MoveNext(out var uid, out var gun))
        {
            if (gun.NextFire > Timing.CurTime)
                continue;

            if (TryComp(uid, out AutoShootGunComponent? autoShoot))
            {
                if (!autoShoot.Enabled)
                    continue;

                AttemptShoot(uid, gun);
            }
            else if (gun.BurstActivated)
            {
                var parent = TransformSystem.GetParentUid(uid);

                //RMC14 Stop burst fire when inside a bag o holster.
                if (!HasComp<DamageableComponent>(parent) &&
                    Containers.TryGetOuterContainer(uid, Transform(uid), out var outerParent))
                {
                    if (HasComp<DamageableComponent>(outerParent.Owner))
                    {
                        gun.BurstActivated = false;
                        gun.BurstShotsCount = 0;
                        gun.NextFire += TimeSpan.FromSeconds(gun.BurstCooldown);
                        Dirty(uid, gun);
                        continue;
                    }
                }
                //RMC14

                if (HasComp<DamageableComponent>(parent))
                    AttemptShoot(parent, uid, gun, gun.ShootCoordinates ?? new EntityCoordinates(uid, gun.DefaultDirection));
                else
                    AttemptShoot(uid, gun);
            }
        }
    }
}
