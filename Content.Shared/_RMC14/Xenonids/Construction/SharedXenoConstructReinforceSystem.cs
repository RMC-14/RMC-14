using Content.Shared.Damage;
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
        SubscribeLocalEvent<XenoConstructReinforceComponent, DamageModifyEvent>(OnReinforceDamageModify);
    }

    public void Reinforce(EntityUid uid, FixedPoint2 amount, TimeSpan duration)
    {
        var comp = EnsureComp<XenoConstructReinforceComponent>(uid);
        comp.ReinforceAmount = amount;
        comp.Duration = duration;
    }

    private void OnReinforceDamageModify(Entity<XenoConstructReinforceComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        foreach (var type in args.Damage.DamageDict)
        {
            if (args.Damage.DamageDict[type.Key] <= 0)
                continue;

            var modifyStep = FixedPoint2.New(
                Math.Min(ent.Comp.ReinforceAmount.Double(),
                args.Damage.DamageDict[type.Key].Double()));

            args.Damage.DamageDict[type.Key] -= modifyStep;
            ent.Comp.ReinforceAmount -= modifyStep;

            if (ent.Comp.ReinforceAmount <= 0)
            {
                ReinforceRemoved(ent);
                return;
            }
        }
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
