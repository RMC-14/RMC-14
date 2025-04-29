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
        _holoTargeted.ApplyHoloStacks(args.Target, component.Decay, component.Stacks, component.MaxStacks);
    }
}
