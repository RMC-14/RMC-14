using Content.Shared._RMC14.Weapons.Ranged.Brute;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Client._RMC14.Weapons.Ranged.Brute;

public sealed class RMCBruteLauncherSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBruteLauncherComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<RMCBruteLauncherComponent> launcher, ref AttemptShootEvent args)
    {
        if (launcher.Comp.LockComplete)
            return;

        args.Cancelled = true;
    }
}
