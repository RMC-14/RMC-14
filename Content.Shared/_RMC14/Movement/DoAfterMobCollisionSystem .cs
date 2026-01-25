using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class DoAfterMobCollisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveDoAfterComponent, AttemptMobCollideEvent>(OnAttemptMobCollide);
    }

    private void OnAttemptMobCollide(EntityUid uid, ActiveDoAfterComponent component, ref AttemptMobCollideEvent args)
    {
        if (!TryComp<DoAfterComponent>(uid, out var doAfterComp))
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
