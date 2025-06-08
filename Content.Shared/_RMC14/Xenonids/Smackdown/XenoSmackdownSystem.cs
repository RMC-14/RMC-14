using Content.Shared._RMC14.Slow;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Smackdown;

public sealed class XenoSmackdownSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoSmackdownComponent, MeleeHitEvent>(OnSmackdownMelee);
    }

    private void OnSmackdownMelee(Entity<XenoSmackdownComponent> xeno, ref MeleeHitEvent args)
    {
        foreach (var ent in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            if (!(HasComp<RMCSlowdownComponent>(ent) || HasComp<RMCSuperSlowdownComponent>(ent) ||
            HasComp<RMCRootedComponent>(ent) || HasComp<StunnedComponent>(ent) ||
            _standing.IsDown(ent)))
                continue;

            var myDamage = _damageable.TryChangeDamage(ent, _xeno.TryApplyXenoSlashDamageMultiplier(ent, xeno.Comp.Damage), origin: xeno, tool: xeno);
            if (myDamage?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(ent, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { ent }, filter);
            }

            _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

            break;
        }
    }
}
