using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class RMCGunGroupPenaltySystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly CMGunSystem _rmcGun = default!;

    private EntityQuery<GunGroupPenaltyComponent> _gunGroupPenalty;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    public override void Initialize()
    {
        _gunGroupPenalty = GetEntityQuery<GunGroupPenaltyComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<GunGroupPenaltyComponent, GotEquippedHandEvent>(OnGroupSpreadPenaltyEquippedHand);
        SubscribeLocalEvent<GunGroupPenaltyComponent, GotUnequippedHandEvent>(OnGroupSpreadPenaltyUnequippedHand);
        SubscribeLocalEvent<GunGroupPenaltyComponent, GunRefreshModifiersEvent>(OnGroupSpreadPenaltyRefreshModifiers);
        SubscribeLocalEvent<GunGroupPenaltyComponent, AmmoShotEvent>(OnGroupSpreadPenaltyAmmoShot, before: [typeof(CMGunSystem)]);
    }

    private void OnGroupSpreadPenaltyEquippedHand(Entity<GunGroupPenaltyComponent> ent, ref GotEquippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyUnequippedHand(Entity<GunGroupPenaltyComponent> ent, ref GotUnequippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyRefreshModifiers(Entity<GunGroupPenaltyComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_rmcGun.TryGetGunUser(ent, out var user))
            return;

        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held == ent.Owner ||
                !_gunGroupPenalty.HasComp(held))
            {
                continue;
            }

            args.CameraRecoilScalar += ent.Comp.Recoil;
            args.AngleIncrease += ent.Comp.AngleIncrease;
            args.MinAngle += ent.Comp.AngleIncrease / 2;
            args.MaxAngle += ent.Comp.AngleIncrease;
            break;
        }
    }

    private void OnGroupSpreadPenaltyAmmoShot(Entity<GunGroupPenaltyComponent> ent, ref AmmoShotEvent args)
    {
        if (!_rmcGun.TryGetGunUser(ent, out var user))
            return;

        var other = false;
        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held != ent.Owner &&
                _gunGroupPenalty.HasComp(held))
            {
                other = true;
                break;
            }
        }

        if (!other)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_projectileQuery.TryComp(projectile, out var projectileComp))
                continue;

            projectileComp.Damage *= ent.Comp.DamageMultiplier;
        }
    }


    private void RefreshGunHolderModifiers(Entity<GunGroupPenaltyComponent> gun)
    {
        _gun.RefreshModifiers(gun.Owner);
        if (!_rmcGun.TryGetGunUser(gun, out var user))
            return;

        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held != gun.Owner)
                _gun.RefreshModifiers(held);
        }
    }
}
