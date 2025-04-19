using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile.Bone;

public sealed class XenoBoneChipsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBoneChipsComponent, XenoBoneChipsActionEvent>(OnXenoBoneSpursAction);
        SubscribeLocalEvent<SlowedByBoneChipsComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedBySpitRefreshMovement);
    }

    private void OnXenoBoneSpursAction(Entity<XenoBoneChipsComponent> xeno, ref XenoBoneChipsActionEvent args)
    {
        if (args.Handled || args.Coords == null)
            return;

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Coords.Value,
            FixedPoint2.Zero,
            xeno.Comp.ProjectileId,
            null,
            1,
            Angle.Zero,
            xeno.Comp.Speed,
            target: args.Entity
        );
    }

    private void OnSlowedBySpitRefreshMovement(Entity<SlowedByBoneChipsComponent> slowed, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (slowed.Comp.ExpiresAt > _timing.CurTime)
            args.ModifySpeed(slowed.Comp.Multiplier, slowed.Comp.Multiplier);
    }
}
