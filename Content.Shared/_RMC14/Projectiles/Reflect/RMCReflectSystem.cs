using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Projectiles.Reflect;

public sealed partial class RMCReflectSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReflectiveComponent, PreventCollideEvent>(OnProjectileReflect,  before: [typeof(RMCProjectileSystem)]);
        SubscribeLocalEvent<RMCReflectiveComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);

        SubscribeLocalEvent<RMCReflectedProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileReflect(Entity<RMCReflectiveComponent> ent, ref PreventCollideEvent args)
    {
        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            return;

        _rmcProjectile.SetProjectileAccuracy(args.OtherEntity, ent.Comp.Accuracy);
    }

    private void OnProjectileReflectAttempt(Entity<RMCReflectiveComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        var projectileEntity = args.ProjUid;

        var reflectorId = GetNetEntity(ent).Id;

        // Don't reflect multiple times from the same enity.
        if (TryComp(projectileEntity, out RMCReflectedProjectileComponent? reflected) && reflected.ReflectedBy.Contains(reflectorId))
        {
            if (reflectorId == reflected.LastReflectedBy)
                args.Cancelled = true;
            return;
        }

        if (!TryReflectProjectile(projectileEntity, ent, ent.Comp.Angle, ent.Comp.Chance))
            return;

        if (TryComp(projectileEntity, out ProjectileIFFComponent? iff) && iff.Enabled)
        {
            args.Component.Damage *= 0;
            return;
        }

        reflected = EnsureComp<RMCReflectedProjectileComponent>(projectileEntity);
        reflected.Accuracy = ent.Comp.Accuracy;
        reflected.ReflectionMultiplier = ent.Comp.ReflectionMultiplier;
        reflected.ReflectedBy.Add(reflectorId);
        reflected.LastReflectedBy = reflectorId;
        Dirty(projectileEntity, reflected);

        args.Component.IgnoreShooter = false;
        Dirty(projectileEntity, args.Component);

        _rmcProjectile.SetMaxRange(projectileEntity, ent.Comp.Range);

        args.Cancelled = true;
    }

    private void OnProjectileHit(Entity<RMCReflectedProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.ReflectedBy.Count <= 0)
            return;

        args.Damage *= MathF.Pow(ent.Comp.ReflectionMultiplier, ent.Comp.ReflectedBy.Count);
    }

    public bool TryReflectProjectile(EntityUid projectile, EntityUid reflector, Angle reflectionAngle, float reflectChance, PhysicsComponent? physics = null)
    {
        if (!Resolve(projectile, ref physics, false))
            return false;

        var tick = _timing.CurTick.Value;
        var reflectorId = GetNetEntity(reflector).Id;
        var seed = ((long)tick << 32) | (uint)reflectorId;
        var rng = new Xoroshiro64S(seed);
        if (reflectChance < rng.NextFloat(0, 1))
            return false;

        var rotation = Angle.FromDegrees(reflectionAngle.Degrees / 2 * rng.NextFloat(-1f, 1f)).Opposite();
        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var newVelocity = rotation.RotateVec(existingVelocity);

        _physics.SetLinearVelocity(projectile, newVelocity, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = rotation.RotateVec(locRot.ToVec());
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        return true;
    }
}
