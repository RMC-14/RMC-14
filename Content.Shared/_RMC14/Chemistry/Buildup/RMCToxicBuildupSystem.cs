using Content.Shared._RMC14.Armor;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Buildup;

public sealed class RMCToxicBuildupSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCToxicBuildupComponent, CMGetArmorEvent>(OnGetArmor);
    }

    private void OnGetArmor(Entity<RMCToxicBuildupComponent> ent, ref CMGetArmorEvent args)
    {
        args.XenoArmor -= (int) ent.Comp.Magnitude;
    }

    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<RMCToxicBuildupComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + UpdateInterval;
        var delta = (float) UpdateInterval.TotalSeconds;

        var query = EntityQueryEnumerator<RMCToxicBuildupComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * delta, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCToxicBuildupComponent>(uid);
                continue;
            }

            Dirty(uid, comp);
        }
    }
}
