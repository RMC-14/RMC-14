using Content.Shared.Damage;
using Content.Shared.Medical;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Defibrillator;

public sealed class CMDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, TargetDefibrillatedEvent>(OnTargetDefibrillated);
    }

    private void OnTargetDefibrillated(Entity<DamageableComponent> ent, ref TargetDefibrillatedEvent args)
    {
        RemComp<CMRecentlyDefibrillatedComponent>(ent);
        var comp = EnsureComp<CMRecentlyDefibrillatedComponent>(ent);
        comp.RemoveAt = _timing.CurTime + comp.RemoveAfter;
        Dirty(ent, comp);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<CMRecentlyDefibrillatedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time >= comp.RemoveAt)
                RemCompDeferred<CMRecentlyDefibrillatedComponent>(uid);
        }
    }
}
