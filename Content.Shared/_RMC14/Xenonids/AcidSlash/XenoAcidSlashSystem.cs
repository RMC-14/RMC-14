using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.AcidSlash;

public sealed class XenoAcidSlashSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidSlashComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<XenoAcidSlashComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno.Owner, hit))
                continue;

            if (HasComp<XenoComponent>(hit))
                continue;

            if (xeno.Comp.Acid is { } add)
                EntityManager.AddComponents(hit, add);

            break;
        }
    }
}
