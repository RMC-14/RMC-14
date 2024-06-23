using Content.Server.Explosion.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server._RMC14.Trigger;

public sealed class CMTriggerSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OnShootTriggerAmmoTimerComponent, AmmoShotEvent>(OnTriggerTimerAmmoShot);
    }

    private void OnTriggerTimerAmmoShot(Entity<OnShootTriggerAmmoTimerComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            _trigger.HandleTimerTrigger(projectile, null, ent.Comp.Delay, ent.Comp.BeepInterval, ent.Comp.InitialBeepDelay, ent.Comp.BeepSound);
        }
    }
}
