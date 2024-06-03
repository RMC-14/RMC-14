using Content.Shared.Interaction.Events;

namespace Content.Shared._CM14.Weapons.Melee;

public abstract class SharedCMMeleeWeaponSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ImmuneToUnarmedComponent, GettingAttackedAttemptEvent>(OnImmuneToUnarmedGettingAttacked);
    }

    private void OnImmuneToUnarmedGettingAttacked(Entity<ImmuneToUnarmedComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        if (args.Attacker == args.Weapon)
            args.Cancelled = true;
    }
}
