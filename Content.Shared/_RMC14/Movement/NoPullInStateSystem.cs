using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Events;

namespace Content.Shared._RMC14.Movement;

public sealed class NoPullInStateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoPullInStateComponent, PullAttemptEvent>(OnPullAttempt);
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
}
