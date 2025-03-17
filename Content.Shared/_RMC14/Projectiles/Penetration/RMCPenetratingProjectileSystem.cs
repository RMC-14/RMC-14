using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Projectiles.Penetration;

public sealed class RMCPenetratingProjectileSystem : EntitySystem
{
    private const int HardCollisionGroup = (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable);

    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSizeStunSystem _rmcSize = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPenetratingProjectileComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCPenetratingProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<RMCPenetratingProjectileComponent, StartCollideEvent>(OnStartCollide, after: [typeof(SharedProjectileSystem)]);
        SubscribeLocalEvent<RMCPenetratingProjectileComponent, ProjectileHitEvent>(OnProjectileHit);

    }

    private void OnMapInit(Entity<RMCPenetratingProjectileComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ShotFrom = _transform.GetMoverCoordinates(ent);
        Dirty(ent);
    }

    /// <summary>
    ///     Prevent collision with an already hit entity.
    /// </summary>
    private void OnPreventCollide(Entity<RMCPenetratingProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if(!ent.Comp.HitTargets.Contains(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    /// <summary>
    ///     Add the hit target to a list of hit targets that won't be hit another time..
    /// </summary>
    private void OnProjectileHit(Entity<RMCPenetratingProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        ent.Comp.HitTargets.Add(args.Target);
        Dirty(ent);
    }

    private void OnStartCollide(Entity<RMCPenetratingProjectileComponent> ent, ref StartCollideEvent args)
    {
        if(!TryComp(ent, out ProjectileComponent? projectile) || ent.Comp.ShotFrom == null || ent.Comp.HitTargets.Contains(args.OtherEntity))
            return;

        var rangeLoss = ent.Comp.RangeLossPerHit;
        var damageLoss = ent.Comp.DamageMultiplierLossPerHit;
       _rmcSize.TryGetSize(args.OtherEntity, out var size);

        // Apply damage and range loss multipliers depending on target hit.
        if ((args.OtherFixture.CollisionLayer & HardCollisionGroup) != 0)
        {
            // Thick Membranes have a lower multiplier.
            if (TryComp(args.OtherEntity, out OccluderComponent? occluder) &&
                !occluder.Enabled)
            {
                rangeLoss *= ent.Comp.ThickMembraneMultiplier;
                damageLoss *=  ent.Comp.ThickMembraneMultiplier;

                // Normal membranes have an even lower multiplier.
                if (HasComp<XenoStructureUpgradeableComponent>(args.OtherEntity))
                {
                    rangeLoss *= ent.Comp.MembraneMultiplier;
                    damageLoss *=  ent.Comp.MembraneMultiplier;
                }
            }
            else
            {
                rangeLoss *= ent.Comp.WallMultiplier;
                damageLoss *=  ent.Comp.WallMultiplier;
            }
        }
        else if(size >= RMCSizes.Big)
        {
            rangeLoss *= ent.Comp.BigXenoMultiplier;
            damageLoss *=  ent.Comp.BigXenoMultiplier;
        }

        ent.Comp.Range -= rangeLoss;
        Dirty(ent);

        projectile.Damage *= 1 - damageLoss;
        Dirty(ent,projectile);

        var distanceTravelled =
            (_transform.GetMoverCoordinates(ent).Position - ent.Comp.ShotFrom.Value.Position).Length();
        var range = ent.Comp.Range - distanceTravelled;

        // Only allow the projectile to penetrate if it still has range left.
        if (range < 0)
            return;

        projectile.DamagedEntity = false;
        Dirty(ent, projectile);
    }
}
