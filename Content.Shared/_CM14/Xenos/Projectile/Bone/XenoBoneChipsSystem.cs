using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Projectile.Bone;

public sealed class XenoBoneChipsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoProjectileSystem _xenoProjectile = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBoneChipsComponent, XenoBoneChipsActionEvent>(OnXenoBoneSpursAction);

        SubscribeLocalEvent<SlowedByBoneChipsComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedBySpitRefreshMovement);
        SubscribeLocalEvent<SlowedByBoneChipsComponent, EntityUnpausedEvent>(OnSlowedBySpitUnpaused);
    }

    private void OnXenoBoneSpursAction(Entity<XenoBoneChipsComponent> xeno, ref XenoBoneChipsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Target,
            FixedPoint2.Zero,
            xeno.Comp.ProjectileId,
            null,
            1,
            Angle.Zero,
            xeno.Comp.Speed
        );
    }

    private void OnSlowedBySpitRefreshMovement(Entity<SlowedByBoneChipsComponent> slowed, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (slowed.Comp.ExpiresAt > _timing.CurTime)
            args.ModifySpeed(slowed.Comp.Multiplier, slowed.Comp.Multiplier);
    }

    private void OnSlowedBySpitUnpaused(Entity<SlowedByBoneChipsComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.ExpiresAt += args.PausedTime;
    }
}
