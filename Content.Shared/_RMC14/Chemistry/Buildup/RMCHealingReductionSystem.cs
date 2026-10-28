using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Buildup;

public sealed class RMCHealingReductionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        var comp = EnsureComp<RMCHealingReductionComponent>(uid);
        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, maxBuildup);
        comp.DissipationRate = dissipationRate;
        comp.MaxBuildup = maxBuildup;
        Dirty(uid, comp);
    }

    public float GetReduction(EntityUid uid)
    {
        return TryComp<RMCHealingReductionComponent>(uid, out var comp) ? comp.Magnitude : 0f;
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

        var query = EntityQueryEnumerator<RMCHealingReductionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * delta, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCHealingReductionComponent>(uid);
                continue;
            }

            if (HasComp<HumanoidAppearanceComponent>(uid))
            {
                var damage = new DamageSpecifier(_prototype.Index(BruteGroup), comp.DissipationRate * delta);
                _damageable.TryChangeDamage(uid, damage, true, interruptsDoAfters: false);
            }

            Dirty(uid, comp);
        }
    }
}
