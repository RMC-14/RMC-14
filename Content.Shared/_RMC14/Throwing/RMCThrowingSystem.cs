using Content.Shared.Damage.Components;
using Content.Shared._RMC14.Deferred;
using Content.Shared.Throwing;

namespace Content.Shared._RMC14.Throwing;

public sealed class RMCThrowingSystem : EntitySystem
{
    [Dependency] private RMCDeferredPhysicsSystem _deferredPhysics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrownEvent>(OnDamageOtherOnHitThrown);
        SubscribeLocalEvent<ThrownLimitHitsComponent, ThrowDoHitEvent>(OnThrownLimitHitsDoHit);
        SubscribeLocalEvent<ThrownLimitHitsComponent, LandEvent>(OnThrownLimitHitsLand);
        SubscribeLocalEvent<ThrownLimitHitsComponent, StopThrowEvent>(OnThrownLimitHitsStopThrow);
    }

    private void OnDamageOtherOnHitThrown(Entity<DamageOtherOnHitComponent> ent, ref ThrownEvent args)
    {
        var limit = EnsureComp<ThrownLimitHitsComponent>(ent);
        limit.Hit = false;
        Dirty(ent, limit);
    }

    private void OnThrownLimitHitsLand(Entity<ThrownLimitHitsComponent> ent, ref LandEvent args)
    {
        ent.Comp.Hit = false;
        Dirty(ent);
    }

    private void OnThrownLimitHitsDoHit(Entity<ThrownLimitHitsComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!_deferredPhysics.TryQueueStopThrow(ent))
            return;

        ent.Comp.Hit = true;
        Dirty(ent);
    }

    private void OnThrownLimitHitsStopThrow(Entity<ThrownLimitHitsComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred<ThrownLimitHitsComponent>(ent);
    }
}
