using Content.Shared._RMC14.Shields;
using Content.Shared.Damage;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

public sealed class XenoSpikeShieldSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoShardSystem _shard = default!;

    public bool TryActivateShield(Entity<XenoSpikeShieldComponent, XenoShardComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return false;

        var time = _timing.CurTime;
        if (ent.Comp1.CooldownExpireAt > time)
            return false;

        if (!_shard.TryConsumeShards((ent.Owner, ent.Comp2), ent.Comp1.ShardCost))
            return false;

        ent.Comp1.ShieldExpireAt = time + ent.Comp1.ShieldDuration;
        ent.Comp1.CooldownExpireAt = time + ent.Comp1.Cooldown;

        EnsureComp<XenoShieldComponent>(ent);
        Dirty(ent.Owner, ent.Comp1);
        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoSpikeShieldComponent, XenoShieldComponent>();
        while (query.MoveNext(out var uid, out var spike, out var shield))
        {
            if (spike.ShieldExpireAt <= time)
            {
                RemComp<XenoShieldComponent>(uid);
            }
        }
    }
}
