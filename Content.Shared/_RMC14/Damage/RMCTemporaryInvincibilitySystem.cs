using Content.Shared._RMC14.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage;

public sealed class RMCTemporaryInvincibilitySystem : EntitySystem
{

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, RMCIgniteAttemptEvent>(OnIgnite);
        SubscribeLocalEvent<RMCTemporaryInvincibilityComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
    }

    private void OnIgnite(Entity<RMCTemporaryInvincibilityComponent> ent, ref RMCIgniteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeDamage(Entity<RMCTemporaryInvincibilityComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
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
        }
    }
}
