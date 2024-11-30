using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Finesse;

public sealed class XenoFinesseSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoFinesseComponent, MeleeHitEvent>(OnFinesseMeleeHit);
    }

    private void OnFinesseMeleeHit(Entity<XenoFinesseComponent> xeno, ref MeleeHitEvent args)
    {
        foreach (var ent in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            var comp = EnsureComp<XenoMarkedComponent>(ent);
            comp.WearOffAt = _timing.CurTime + xeno.Comp.MarkedTime;
            comp.TimeAdded = _timing.CurTime;

            return;
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var markedQuery = EntityQueryEnumerator<XenoMarkedComponent>();

        while (markedQuery.MoveNext(out var uid, out var mark))
        {
            if (time < mark.WearOffAt)
                continue;

            RemCompDeferred<XenoMarkedComponent>(uid);
        }
    }
}
