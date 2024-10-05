using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Trigger;

public sealed class RMCTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OnShootTriggerAmmoTimerComponent, AmmoShotEvent>(OnTriggerTimerAmmoShot);
        SubscribeLocalEvent<TriggerOnFixedDistanceStopComponent, ProjectileFixedDistanceStopEvent>(OnTriggerOnFixedDistanceStop);
    }

    private void OnTriggerTimerAmmoShot(Entity<OnShootTriggerAmmoTimerComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            _trigger.HandleTimerTrigger(projectile, null, ent.Comp.Delay, ent.Comp.BeepInterval, ent.Comp.InitialBeepDelay, ent.Comp.BeepSound);
        }
    }

    private void OnTriggerOnFixedDistanceStop(Entity<TriggerOnFixedDistanceStopComponent> ent, ref ProjectileFixedDistanceStopEvent args)
    {
        var active = EnsureComp<ActiveTriggerOnThrowEndComponent>(ent);
        active.TriggerAt = _timing.CurTime + ent.Comp.Delay;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveTriggerOnThrowEndComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (time < active.TriggerAt)
                continue;

            _trigger.Trigger(uid);
            if (!EntityManager.IsQueuedForDeletion(uid) && !TerminatingOrDeleted(uid))
                QueueDel(uid);
        }
    }
}
