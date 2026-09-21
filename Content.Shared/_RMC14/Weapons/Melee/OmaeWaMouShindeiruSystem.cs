using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public sealed class OmaeWaMouShindeiruSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<OmaeWaMouShindeiruComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (_net.IsClient)
            return;

        var damage = _melee.GetDamage(ent.Owner, args.User);
        foreach (var target in args.HitEntities)
        {
            // TODO Make both katana wielders attack each other?
            if (_hands.IsHolding(args.User, ent.Owner) || _hands.IsHolding(target, ent.Owner) && args.User != target)
            {
                _stun.TryStun(args.User, ent.Comp.KillDelay, true);
                _stun.TryStun(target, ent.Comp.KillDelay, true);
            }
            else
            {
                _stun.TryStun(target, ent.Comp.KillDelay, true);
            }

            if (!ent.Comp.PendingTargets.Add(target))
                continue;

            OmaeWaMouShindeiru(ent, args.User, target, damage);
        }

        if (!ent.Comp.DamageOnHit)
            args.Handled = true;
    }

    private void OmaeWaMouShindeiru(Entity<OmaeWaMouShindeiruComponent> ent, EntityUid user, EntityUid target, DamageSpecifier damage)
    {
        Timer.Spawn(ent.Comp.KillDelay,
            () =>
        {
            ent.Comp.PendingTargets.Remove(target);

            if (Deleted(ent.Owner) || Deleted(target))
                return;

            if (!Exists(target))
                return;

            if (TryComp<MobStateComponent>(target, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
            {
                return;
            }

            // TODO RMC14 hit outer limbs to decap/delimb
            // var/def_zone = pick("head","l_leg","l_foot","r_leg","r_foot","l_arm","l_hand","r_arm","r_hand")
            for (var i = 0; i < ent.Comp.NumberOfCuts; i++)
            {
                _damageable.TryChangeDamage(target, damage, true, origin: user, tool: ent.Owner);
            }
        });
    }
}
