using Content.Server.Explosion.Components;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Explosion.EntitySystems;

public sealed class RMCProjectileGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileGrenadeComponent, ProjectileHitEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileGrenadeComponent, FragmentIntoProjectilesEvent>(OnFragmentIntoProjectiles);
    }

    /// <summary>
    /// Reverses the payload shooting direction if the projectile grenade collides with an entity
    /// </summary>
    private void OnStartCollide(Entity<ProjectileGrenadeComponent> entity, ref ProjectileHitEvent args)
    {
        if (!entity.Comp.Rebounds)
            return;

        //Shoot the payload backwards if colliding with an entity
        entity.Comp.DirectionAngle += entity.Comp.ReboundAngle;

        var ev = new RMCProjectileReboundEvent(entity.Comp.ReboundAngle);
        RaiseLocalEvent(entity, ref ev);

        _trigger.Trigger(entity);
    }

    /// <summary>
    /// Overwrites the logic of the upstream <seealso cref="ProjectileGrenadeSystem"/> to allow more customization
    /// </summary>
    private void OnFragmentIntoProjectiles(Entity<ProjectileGrenadeComponent> ent, ref FragmentIntoProjectilesEvent args)
    {
        args.Handled = true;

        var segmentAngle = ent.Comp.SpreadAngle / args.TotalCount;
        var projectileRotation = _transformSystem.GetMoverCoordinateRotation(ent.Owner, Transform(ent.Owner)).worldRot.Degrees + ent.Comp.DirectionAngle;

        // Give the same IFF faction and enabled state to the projectiles shot from the grenade
        if (ent.Comp.InheritIFF)
        {
            if (TryComp(ent.Owner, out ProjectileIFFComponent? grenadeIFFComponent))
            {
                _gunIFF.GiveAmmoIFF(args.ContentUid, grenadeIFFComponent.Faction, grenadeIFFComponent.Enabled);
            }
        }

        var angleMin = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * args.ShootCount;
        var angleMax = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * (args.ShootCount + 1);

        if (ent.Comp.EvenSpread)
            args.Angle = Angle.FromDegrees((angleMin + angleMax) / 2);
        else
            args.Angle = Angle.FromDegrees(_random.Next((int)angleMin, (int)angleMax));
    }
}

/// <summary>
///     Raised when a projectile grenade is being triggered
/// </summary>
[ByRefEvent]
public record struct FragmentIntoProjectilesEvent(EntityUid ContentUid, int TotalCount, Angle Angle, int ShootCount, bool Handled = false);
