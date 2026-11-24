using Content.Shared.Damage.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Throwing;

public sealed class RMCThrowingSystem : EntitySystem
{
    [Dependency] private readonly ThrownItemSystem _thrown = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrownEvent>(OnDamageOtherOnHitThrown);
        SubscribeLocalEvent<ThrownLimitHitsComponent, ThrowDoHitEvent>(OnThrownLimitHitsDoHit);
        SubscribeLocalEvent<ThrownLimitHitsComponent, LandEvent>(OnThrownLimitHitsLand);
        SubscribeLocalEvent<ThrownLimitHitsComponent, StopThrowEvent>(OnThrownLimitHitsStopThrow);

        SubscribeLocalEvent<RMCThrowCooldownComponent, ThrowItemAttemptEvent>(OnThrowCooldownThrowAttempt);
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
        ent.Comp.Hit = true;
        Dirty(ent);

        if (_thrownItemQuery.TryComp(ent, out var thrown))
            _thrown.StopThrow(ent, thrown);
    }

    private void OnThrownLimitHitsStopThrow(Entity<ThrownLimitHitsComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred<ThrownLimitHitsComponent>(ent);
    }

    private void OnThrowCooldownThrowAttempt(Entity<RMCThrowCooldownComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_timing.CurTime < ent.Comp.CooldownEndsAt)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("throwing-user-threw-fail-cooldown", ("item", Identity.Name(ent, EntityManager, args.User))), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        ent.Comp.CooldownEndsAt = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
    }
}
