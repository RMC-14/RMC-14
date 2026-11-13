using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.AcidSlash;

public sealed class XenoAcidSlashSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSpitSystem _spit = default!;
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

            _spit.ApplyOrExtendAcid(hit, xeno.Comp.Acid);

            break;
        }
    }
}
