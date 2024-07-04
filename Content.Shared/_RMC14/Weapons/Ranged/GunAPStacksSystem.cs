using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public abstract class GunAPStacksSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GunAPStacksMofifierComponent, AmmoShotEvent>(OnGunShot);
        SubscribeLocalEvent<PumpActionComponent, AttemptShootEvent>(OnAttemptShoot);
    }
    private void OnGunShot()
    {
        //Apply AP to the bullet
    }
}
