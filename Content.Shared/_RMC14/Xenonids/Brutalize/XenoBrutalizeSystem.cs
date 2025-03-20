using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared._RMC14.Xenonids.Charge;
using Content.Shared.Actions;
using Content.Shared._RMC14.Shields;

namespace Content.Shared._RMC14.Xenonids.Brutalize;

public sealed class XenoBrutalizeSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBrutalizeComponent, MeleeHitEvent>(OnBrutalMeleeHit);
    }

    private void OnBrutalMeleeHit(Entity<XenoBrutalizeComponent> xeno, ref MeleeHitEvent args)
    {
        EntityUid? mainTarget = null;
        foreach (var ent in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            mainTarget = ent;
            break;
        }

        if (mainTarget == null)
            return;

        int currHits = 0;
        var damage = args.BaseDamage * xeno.Comp.AOEDamageMult;

        foreach (var extra in _entityLookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(mainTarget.Value), xeno.Comp.Range))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, extra) || _mobState.IsDead(extra))
                continue;

            if (args.HitEntities.Contains(extra))
                continue;

            currHits++;

            var myDamage = _damageable.TryChangeDamage(extra, damage);
            if (myDamage?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(extra, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { extra }, filter);
            }

            if (xeno.Comp.MaxTargets != null && currHits >= xeno.Comp.MaxTargets)
                break;

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, extra.Owner.ToCoordinates());
        }

        RefreshCooldowns(xeno, currHits);
    }

    private void RefreshCooldowns(Entity<XenoBrutalizeComponent> xeno, int hits)
    {
        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if ((action.BaseEvent is XenoChargeActionEvent || action.BaseEvent is XenoDefensiveShieldActionEvent)
                && action.Cooldown != null)
            {
                //Additional cooldown only on Charge
                var cooldownEnd = action.Cooldown.Value.End - (xeno.Comp.BaseCooldownReduction +
                    (action.BaseEvent is XenoChargeActionEvent ? hits * xeno.Comp.AddtionalCooldownReductions : TimeSpan.Zero));

                if (cooldownEnd < action.Cooldown.Value.Start)
                    _actions.ClearCooldown(actionId);
                else
                    _actions.SetCooldown(actionId, action.Cooldown.Value.Start, cooldownEnd);
            }
        }
    }
}
