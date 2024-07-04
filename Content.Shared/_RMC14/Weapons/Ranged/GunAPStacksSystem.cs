using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public abstract class GunAPStacksSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GunAPStacksModifierComponent, AmmoShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunAPStacksModifierComponent, ProjectileEmbedEvent>(ChangeStack);
    }
    private void OnGunShot()
    {
        //Apply AP to the bullet
        
    }
    private void ChangeStack(EntityUid? shooter, EntityUid weapon, EntityUid target)
    {
        //If xenoid hit increase stack
        //if anything else decrease stack
    }
}
