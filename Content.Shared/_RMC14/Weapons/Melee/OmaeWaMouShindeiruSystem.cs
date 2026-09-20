using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public sealed class OmaeWaMouShindeiruSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly INetManager _net = default!;

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

        // Get the weapon's damage at the moment of impact
        var damage = _melee.GetDamage(ent.Owner, args.User);
        foreach (var target in args.HitEntities)
        {
            // Don't allow multiple delayed executions against the same target while one is already pending
            if (!ent.Comp.PendingTargets.Add(target))
                continue;

            ScheduleDelayedAttack(ent, args.User, target, damage);
        }

        if (!ent.Comp.DamageOnHit)
            args.Handled = true;
    }

    private void ScheduleDelayedAttack(Entity<OmaeWaMouShindeiruComponent> ent, EntityUid user, EntityUid target, DamageSpecifier damage)
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

            ExecuteCuts(ent, user, target, damage);
        });
    }

    private void ExecuteCuts(Entity<OmaeWaMouShindeiruComponent> ent, EntityUid user, EntityUid target, DamageSpecifier damage)
    {
        for (var i = 0; i < ent.Comp.NumberOfCuts; i++)
        {
            _damageable.TryChangeDamage(target, damage, origin: user, tool: ent.Owner);
        }
    }
}
