using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage.TimedInvincibility;

public sealed class RMCTimedInvincibilitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _queuedInvincibleEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<RMCTimedInvincibilityComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<RMCTimedInvincibilityComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        _queuedInvincibleEntities.Clear();

        var query = EntityQueryEnumerator<RMCTimedInvincibilityComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            if (comp.Lifetime <= 0)
            {
                _queuedInvincibleEntities.Add(uid);
            }
        }

        foreach (var queued in _queuedInvincibleEntities)
        {
            RemComp<RMCTimedInvincibilityComponent>(queued);
        }
    }
}
