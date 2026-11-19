using Content.Shared._RMC14.Movement;
using Content.Shared.DoAfter;

namespace Content.Shared.Movement.Systems;

public sealed class DoAfterMobCollisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveDoAfterComponent, RMCAttemptMobPushedEvent>(OnAttemptMobPushed);
    }

    private void OnAttemptMobPushed(Entity<ActiveDoAfterComponent> ent, ref RMCAttemptMobPushedEvent args)
    {
        if (!TryComp<DoAfterComponent>(ent, out var doAfterComp))
            return;

        foreach (var doAfter in doAfterComp.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed)
                continue;

            if (doAfter.Args.RootEntity)
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
