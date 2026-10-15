using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Evasion;
using Content.Shared.Damage;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage;

public sealed class RMCTemporaryInvincibilitySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EvasionSystem _evasion = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, RMCIgniteAttemptEvent>(OnIgnite);
        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, EvasionRefreshModifiersEvent>(OnGetEvasion);
        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, ComponentStartup>(OnAdded);
    }

    private void OnAdded(Entity<RMCTemporaryInvincibilityComponent> ent, ref ComponentStartup args)
    {
        _evasion.RefreshEvasionModifiers(ent);
    }

    private void OnIgnite(Entity<RMCTemporaryInvincibilityComponent> ent, ref RMCIgniteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeDamage(Entity<RMCTemporaryInvincibilityComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetEvasion(Entity<RMCTemporaryInvincibilityComponent> ent, ref EvasionRefreshModifiersEvent args)
    {
        args.Evasion += 1000; // Bullets need to always miss
    }

    /// <summary>
    ///     Grants an entity temporary invincibility (blocks damage and ignition, and makes projectiles miss) for the
    ///     given duration. This is the single entry point all larva spawn/respawn paths use so the behavior stays
    ///     consistent. Calling again refreshes the expiry; a duration of zero or less does nothing.
    /// </summary>
    public void GiveInvincibility(EntityUid uid, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        var invincibility = EnsureComp<RMCTemporaryInvincibilityComponent>(uid);
        invincibility.ExpiresAt = _timing.CurTime + duration;
        Dirty(uid, invincibility);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var invQuery = EntityQueryEnumerator<RMCTemporaryInvincibilityComponent>();

        while (invQuery.MoveNext(out var uid, out var comp))
        {
            if (time < comp.ExpiresAt)
                continue;

            RemCompDeferred<RMCTemporaryInvincibilityComponent>(uid);
            _evasion.RefreshEvasionModifiers(uid);
        }
    }
}
