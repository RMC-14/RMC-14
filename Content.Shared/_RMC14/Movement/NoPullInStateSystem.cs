using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Shared._RMC14.Movement;

public sealed class NoPullInStateSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoPullInStateComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<NoPullInStateComponent, MobStateChangedEvent>(OnStateChanged);
    }

    private void OnPullAttempt(Entity<NoPullInStateComponent> ent, ref PullAttemptEvent args)
    {
        var (uid, comp) = ent;
        // only care when this mob is what's being pulled
        if (uid != args.PulledUid)
            return;

        if (TryComp<MobStateComponent>(uid, out var state) && state.CurrentState == comp.State)
            args.Cancelled = true;
    }

    private void OnStateChanged(Entity<NoPullInStateComponent> ent, ref MobStateChangedEvent args)
    {
        var (uid, comp) = ent;
        if (args.NewMobState == comp.State && TryComp<PullableComponent>(uid, out var pullable))
            _pulling.TryStopPull(uid, pullable);
    }
}
