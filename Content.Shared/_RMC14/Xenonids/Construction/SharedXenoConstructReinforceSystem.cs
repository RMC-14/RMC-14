using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction;

public sealed class SharedXenoConstructReinforceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoConstructReinforceComponent, DamageModifyEvent>(OnReinforceDamageModify, after: [typeof(SharedRMCMeleeWeaponSystem)]);
        SubscribeLocalEvent<XenoConstructReinforceComponent, BeforeExplodeEvent>(OnReinforceBeforeExplode);
    }

    public void Reinforce(EntityUid uid, FixedPoint2 amount, TimeSpan duration)
    {
        var comp = EnsureComp<XenoConstructReinforceComponent>(uid);
        comp.ReinforceAmount = amount;
        comp.Duration = duration;
    }

    private void ReduceDamage(Entity<XenoConstructReinforceComponent> ent, ref DamageSpecifier damage)
    {
        if (!damage.AnyPositive())
            return;

        damage = new DamageSpecifier(damage);
        foreach (var type in damage.DamageDict)
        {
            if (damage.DamageDict[type.Key] <= 0)
                continue;

            var modifyStep = FixedPoint2.New(
                Math.Min(ent.Comp.ReinforceAmount.Double(),
                damage.DamageDict[type.Key].Double()));

            damage.DamageDict[type.Key] -= modifyStep;
            ent.Comp.ReinforceAmount -= modifyStep;

            if (ent.Comp.ReinforceAmount <= 0)
            {
                ReinforceRemoved(ent);
                break;
            }
        }
    }

    private void OnReinforceBeforeExplode(Entity<XenoConstructReinforceComponent> ent, ref BeforeExplodeEvent args)
    {
        ReduceDamage(ent, ref args.Damage);
    }

    private void OnReinforceDamageModify(Entity<XenoConstructReinforceComponent> ent, ref DamageModifyEvent args)
    {
        ReduceDamage(ent, ref args.Damage);
    }

    private void ReinforceRemoved(Entity<XenoConstructReinforceComponent> ent)
    {
        RemCompDeferred<XenoConstructReinforceComponent>(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var reinforceQuery = EntityQueryEnumerator<XenoConstructReinforceComponent>();

        while (reinforceQuery.MoveNext(out var uid, out var comp))
        {
            comp.EndAt ??= time + comp.Duration;

            if (time < comp.EndAt)
                continue;

            ReinforceRemoved((uid, comp));
        }
    }
}
