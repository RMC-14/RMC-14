using Content.Shared.Projectiles;

namespace Content.Shared._RMC14.Weapons.Ranged.HoloTargeting;

public sealed class RMCHoloTargetingSystem : EntitySystem
{
    [Dependency] private readonly RMCHoloTargetedSystem _holoTargeted = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoloTargetingComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid uid, HoloTargetingComponent component, ref ProjectileHitEvent args)
    {
        _holoTargeted.TryApplyHoloStacks(args.Target, component.Duration, component.Stacks, component.MaxStacks);
    }
}
