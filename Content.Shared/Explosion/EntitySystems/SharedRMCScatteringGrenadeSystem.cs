using Content.Shared.Explosion.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Shared.Explosion.EntitySystems;

public sealed class RMCSharedScatteringGrenadeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScatteringGrenadeComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ScatteringGrenadeComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ScatteringGrenadeComponent, ScatterGrenadeContentsEvent>(OnScatterGrenadeContents);
    }

    /// <summary>
    /// Scatters the grenades in the given angle.
    /// </summary>
    private void OnScatterGrenadeContents(Entity<ScatteringGrenadeComponent> ent, ref ScatterGrenadeContentsEvent args)
    {
        var projectileRotation = _transformSystem.GetMoverCoordinateRotation(ent.Owner, Transform(ent.Owner)).worldRot.Degrees + ent.Comp.DirectionAngle;
        var spreadAngle = ent.Comp.SpreadAngle / args.TotalCount;

        var angleMin = projectileRotation - ent.Comp.SpreadAngle / 2 + spreadAngle * args.ThrownCount;
        var angleMax = projectileRotation - ent.Comp.SpreadAngle / 2 + spreadAngle * (args.ThrownCount + 1);

        args.Angle = Angle.FromDegrees(_random.Next((int)angleMin, (int)angleMax));
        args.Handled = true;
    }

    /// <summary>
    /// Triggers the scattering grenade if it collides with a wall
    /// </summary>
    private void OnStartCollide(Entity<ScatteringGrenadeComponent> ent, ref StartCollideEvent args)
    {
        if ((args.OtherFixture.CollisionLayer & (int)(CollisionGroup.Impassable | CollisionGroup.HighImpassable)) ==
            0 || !ent.Comp.TriggerOnWallCollide)
            return;

        ent.Comp.IsTriggered = true;
        Dirty(ent);

    }

    /// <summary>
    /// Triggers the scattering grenade if hits any entity and makes the content bounce back
    /// </summary>
    private void OnProjectileHit(Entity<ScatteringGrenadeComponent> ent, ref ProjectileHitEvent args)
    {
        if(!ent.Comp.DirectHitTrigger)
            return;

        ent.Comp.DirectionAngle += ent.Comp.ReboundAngle;
        ent.Comp.IsTriggered = true;
        Dirty(ent);
    }
}

/// <summary>
///     Raised when a scattering grenade is being triggered.
/// </summary>
[ByRefEvent]
public record struct ScatterGrenadeContentsEvent(int TotalCount, int ThrownCount, Angle Angle, bool Handled = false);

/// <summary>
///     Raised when a the content of a scattering grenade is being thrown.
/// </summary>
[ByRefEvent]
public record struct GrenadeContentThrownEvent(EntityUid Source);
