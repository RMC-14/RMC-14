using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Projectiles;
using Content.Shared._RMC14.Armor;

namespace Content.Shared._RMC14.Weapons.Ranged;

public abstract class GunAPStacksSystem : EntitySystem
{
    //[Dependency] private readonly CMArmorSystem _armor = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<GunAPStacksModifierComponent, AmmoShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunAPStacksModifierComponent, ProjectileEmbedEvent>(ChangeStack);
        SubscribeLocalEvent<CMArmorPiercingComponent, GetGunAPStacksModifierEvent>(GiveAmmoAP);
    }
    private void OnGunShot(Entity<GunAPStacksModifierComponent> ent, ref AmmoShotEvent args)
    {
        //Apply AP to the bullet
        ent.Comp.AP = ent.Comp.Stacks * 10;
        if(ent.Comp.AP > 50)
        {
            ent.Comp.AP = 50;
            
        }
        foreach(var bullet in args.FiredProjectiles)
        {
            if(HasComp<CMArmorPiercingComponent>(bullet))
            {
                var ev = new GetGunAPStacksModifierEvent(ent.Comp.AP);
                RaiseLocalEvent(bullet, ref ev);
            }
        }
        
        Dirty(ent);
    }
    private void ChangeStack(Entity<GunAPStacksModifierComponent> ent, ref ProjectileEmbedEvent args)
    //EntityUid? shooter, EntityUid weapon, EntityUid target
    {
        //If xenoid hit increase stack
        if(HasComp<XenoComponent>(args.Embedded))
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
        //_armor.SetArmorPiercing(args.Weapon, (ent.Comp.Stacks * 10));
    }
    private void GiveAmmoAP(Entity<CMArmorPiercingComponent> ent, ref GetGunAPStacksModifierEvent args)
    {
        ent.Comp.Amount = args.Stacks;
        Dirty(ent);
    }
}
