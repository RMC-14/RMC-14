using Content.Shared._RMC14.Movement;
using Content.Shared.DoAfter;

namespace Content.Shared.Movement.Systems;

public sealed class DoAfterMobCollisionSystem : EntitySystem
{
    [Dependency] private readonly RMCImmobileActionSystem _immobileAction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveDoAfterComponent, RMCAttemptMobPushedEvent>(OnAttemptMobPushed);
    }

    private void OnAttemptMobPushed(EntityUid uid, ActiveDoAfterComponent component, ref RMCAttemptMobPushedEvent args)
    {
        if (!TryComp<DoAfterComponent>(uid, out var doAfterComp))
            return;

        foreach (var doAfter in doAfterComp.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed)
                continue;

            if (doAfter.Args.RootEntity)
            {
                if (_immobileAction.BlocksPush(uid, args.Pusher))
                    args.Cancelled = true;

                return;
            }
        }
    }
}
