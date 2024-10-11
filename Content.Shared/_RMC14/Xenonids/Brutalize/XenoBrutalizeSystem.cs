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

            if (currHits >= xeno.Comp.MaxTargets)
                break;

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, extra.Owner.ToCoordinates());
        }

        if (!TryComp<XenoComponent>(xeno, out var act))
            return;

        if (TryComp<InstantActionComponent>(act.Actions[xeno.Comp.BaseCooldownAction], out var basAct) && basAct.Cooldown != null)
            _actions.SetCooldown(act.Actions[xeno.Comp.BaseCooldownAction], basAct.Cooldown.Value.Start, basAct.Cooldown.Value.End - xeno.Comp.BaseCooldownReduction);


        if (TryComp<WorldTargetActionComponent>(act.Actions[xeno.Comp.CummulativeCooldownAction], out var culAct) && culAct.Cooldown != null)
            _actions.SetCooldown(act.Actions[xeno.Comp.CummulativeCooldownAction], culAct.Cooldown.Value.Start, culAct.Cooldown.Value.End - (xeno.Comp.BaseCooldownReduction + currHits
             * xeno.Comp.AddtionalCooldownReductions));
    }
}
