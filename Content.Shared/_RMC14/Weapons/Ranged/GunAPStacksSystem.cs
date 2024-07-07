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
    private void OnGunShot(Entity<GunAPStacksModifierComponent> ent)
    {
        //Apply AP to the bullet
        ent.Comp.AP = ent.Comp.Stacks * 10;
        if(ent.Comp.AP > 50)
        {
            ent.Comp.AP = 50;
            
        }
        
        Dirty(ent);
    }
    private void ChangeStack(Entity<GunAPStacksModifierComponent> ent, EntityUid? shooter, EntityUid weapon, EntityUid target)
    {
        //If xenoid hit increase stack
        if(HasComp<XenoComponent>(target))
        {
            if(ent.Comp.Stacks < 5)//stacks cap at 50ap or 5 stacks
            {
            ent.Comp.Stacks++;
            }
        }
        //if anything else decrease stack
        else
        {
            if(ent.Comp.Stacks != 0)
            {
                ent.Comp.Stacks--;
            }
        }
        Dirty(ent);
    }
}
