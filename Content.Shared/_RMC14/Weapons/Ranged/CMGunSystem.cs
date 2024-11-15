using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class CMGunSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectileSystem = default!;

    private EntityQuery<GunGroupSpreadPenaltyComponent> _gunGroupSpreadPenalty;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    private readonly int _blockArcCollisionGroup = (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable);

    public override void Initialize()
    {
        _gunGroupSpreadPenalty = GetEntityQuery<GunGroupSpreadPenaltyComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<ShootAtFixedPointComponent, AmmoShotEvent>(OnShootAtFixedPointShot);

        SubscribeLocalEvent<RMCWeaponDamageFalloffComponent, AmmoShotEvent>(OnWeaponDamageFalloffShot);
        SubscribeLocalEvent<RMCWeaponDamageFalloffComponent, GunRefreshModifiersEvent>(OnWeaponDamageFalloffRefreshModifiers);

        SubscribeLocalEvent<RMCExtraProjectilesDamageModsComponent, AmmoShotEvent>(OnExtraProjectilesShot);

        SubscribeLocalEvent<ProjectileFixedDistanceComponent, PreventCollideEvent>(OnCollisionCheckArc);
        SubscribeLocalEvent<ProjectileFixedDistanceComponent, PhysicsSleepEvent>(OnEventToStopProjectile);

        SubscribeLocalEvent<GunShowUseDelayComponent, GunShotEvent>(OnShowUseDelayShot);
        SubscribeLocalEvent<GunShowUseDelayComponent, ItemWieldedEvent>(OnShowUseDelayWielded);

        SubscribeLocalEvent<GunUserWhitelistComponent, AttemptShootEvent>(OnGunUserWhitelistAttemptShoot);

        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GotEquippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GotUnequippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GunRefreshModifiersEvent>(OnGunUnskilledPenaltyRefresh);

        SubscribeLocalEvent<GunDamageModifierComponent, AmmoShotEvent>(OnGunDamageModifierAmmoShot);
        SubscribeLocalEvent<GunDamageModifierComponent, MapInitEvent>(OnGunDamageModifierMapInit);

        SubscribeLocalEvent<GunSkilledRecoilComponent, GotEquippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, GotUnequippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, ItemWieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, ItemUnwieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, GunRefreshModifiersEvent>(OnRecoilSkilledRefreshModifiers);

        SubscribeLocalEvent<GunRequiresSkillsComponent, AttemptShootEvent>(OnRequiresSkillsAttemptShoot);

        SubscribeLocalEvent<GunRequireEquippedComponent, AttemptShootEvent>(OnRequireEquippedAttemptShoot);

        SubscribeLocalEvent<RevolverAmmoProviderComponent, UniqueActionEvent>(OnRevolverUniqueAction);

        SubscribeLocalEvent<UserBlockShootingInsideContainersComponent, ShotAttemptedEvent>(OnUserBlockShootingInsideContainersAttemptShoot);

        SubscribeLocalEvent<RMCAmmoEjectComponent, ActivateInWorldEvent>(OnAmmoEjectActivateInWorld);

        SubscribeLocalEvent<GunGroupSpreadPenaltyComponent, GotEquippedHandEvent>(OnGroupSpreadPenaltyEquippedHand);
        SubscribeLocalEvent<GunGroupSpreadPenaltyComponent, GotUnequippedHandEvent>(OnGroupSpreadPenaltyUnequippedHand);
        SubscribeLocalEvent<GunGroupSpreadPenaltyComponent, GunRefreshModifiersEvent>(OnGroupSpreadPenaltyRefreshModifiers);
        SubscribeLocalEvent<GunGroupSpreadPenaltyComponent, AmmoShotEvent>(OnGroupSpreadPenaltyAmmoShot);
    }

    /// <summary>
    /// Shoot at a targeted point's coordinates. The projectile will stop at that location instead of continuing on until it hits something.
    /// There is also an option to arc the projectile with ShootArcProj or ArcProj = true, making it ignore most collision.
    /// </summary>
    /// <remarks>
    /// For some reason, the engine seem to cause MaxFixedRange's conversion to actual projectile max ranges of around +1 tile.
    /// As a result, conversions should be 1 less than max_range when porting, and the minimum range for this feature is around 2 tiles.
    /// This could be manually tweaked try and fix it, but the math seems like it should be fine and it's predictable enough to be worked around for now.
    /// </remarks>
    private void OnShootAtFixedPointShot(Entity<ShootAtFixedPointComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp(ent, out GunComponent? gun) ||
            gun.ShootCoordinates is not { } target)
        {
            return;
        }

        // Find start and end coordinates for vector.
        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.ToMapCoordinates(target);
        // Must be same map.
        if (from.MapId != to.MapId)
            return;

        // Calculate vector, cancel if it ends up at 0.
        var direction = to.Position - from.Position;
        if (direction == Vector2.Zero)
            return;

        // Check for a max range from the ShootAtFixedPointComponent. If defined, take the minimum between that and the calculated distance.
        var distance = ent.Comp.MaxFixedRange != null ? Math.Min(ent.Comp.MaxFixedRange.Value, direction.Length()) : direction.Length();
        // Get current time and normalize the vector for physics math.
        var time = _timing.CurTime;
        var normalized = direction.Normalized();

        // Send each FiredProjectile with a PhysicsComponent off with the same Vector. Max
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_physicsQuery.TryComp(projectile, out var physics))
                continue;

            // Calculate needed impulse to get to target, remove all velocity from projectile, then apply.
            var impulse = normalized * gun.ProjectileSpeedModified * physics.Mass;
            _physics.SetLinearVelocity(projectile, Vector2.Zero, body: physics);
            _physics.ApplyLinearImpulse(projectile, impulse, body: physics);
            _physics.SetBodyStatus(projectile, physics, BodyStatus.InAir);

            // Apply the ProjectileFixedDistanceComponent onto each fired projectile, which both holds the FlyEndTime to be continually checked
            // and will trigger the OnEventToStopProjectile function once the PFD Component is deleted at that time. See Update()
            var comp = EnsureComp<ProjectileFixedDistanceComponent>(projectile);

            // Transfer arcing to the projectile.
            if (Comp<ShootAtFixedPointComponent>(ent).ShootArcProj)
                comp.ArcProj = true;

            // Take the lowest nonzero MaxFixedRange between projectile and gun for the capped vector length.
            if (TryComp(projectile, out ProjectileComponent? normalProjectile) && normalProjectile.MaxFixedRange > 0)
            {
                distance = distance > 0 ? Math.Min(normalProjectile.MaxFixedRange.Value, distance) : normalProjectile.MaxFixedRange.Value;
            }
            // Calculate travel time and equivalent distance based either on click location or calculated max range, whichever is shorter.
            comp.FlyEndTime = time + TimeSpan.FromSeconds(distance / gun.ProjectileSpeedModified);
        }
    }

    /// <summary>
    /// If the projectile collides with anything that doesn't have CollisionGroup.Impassable like walls, and it's arcing, ignore the collision.
    /// </summary>
    private void OnCollisionCheckArc(Entity<ProjectileFixedDistanceComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.ArcProj && (args.OtherFixture.CollisionLayer & _blockArcCollisionGroup) == 0)
            args.Cancelled = true;
    }

    private void OnEventToStopProjectile<T>(Entity<ProjectileFixedDistanceComponent> ent, ref T args)
    {
        StopProjectile(ent);
    }

    private void OnWeaponDamageFalloffRefreshModifiers(Entity<RMCWeaponDamageFalloffComponent> weapon, ref GunRefreshModifiersEvent args)
    {
        var ev = new GetDamageFalloffEvent(weapon.Comp.FalloffMultiplier);
        RaiseLocalEvent(weapon.Owner, ref ev);

        weapon.Comp.ModifiedFalloffMultiplier = FixedPoint2.Max(ev.FalloffMultiplier, 0);

        Dirty(weapon);
    }

    private void OnWeaponDamageFalloffShot(Entity<RMCWeaponDamageFalloffComponent> weapon, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp(projectile, out RMCProjectileDamageFalloffComponent? falloffComponent))
                continue;

            _rmcProjectileSystem.SetProjectileFalloffWeaponMult((projectile, falloffComponent), weapon.Comp.ModifiedFalloffMultiplier);
        }
    }

    private void OnExtraProjectilesShot(Entity<RMCExtraProjectilesDamageModsComponent> weapon, ref AmmoShotEvent args)
    {
        for (int t = 1; t < args.FiredProjectiles.Count; ++t)
        {
            if (!TryComp(args.FiredProjectiles[t], out ProjectileComponent? projectileComponent))
                continue;

            projectileComponent.Damage *= weapon.Comp.DamageMultiplier;
        }
    }

    private void OnShowUseDelayShot(Entity<GunShowUseDelayComponent> ent, ref GunShotEvent args)
    {
        UpdateDelay(ent);
    }

    private void OnShowUseDelayWielded(Entity<GunShowUseDelayComponent> ent, ref ItemWieldedEvent args)
    {
        UpdateDelay(ent);
    }

    private void OnGunUserWhitelistAttemptShoot(Entity<GunUserWhitelistComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<BypassInteractionChecksComponent>(args.User))
            return;

        if (_whitelist.IsValid(ent.Comp.Whitelist, args.User))
            return;

        args.Cancelled = true;

        var popup = Loc.GetString("cm-gun-unskilled", ("gun", ent.Owner));
        _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
    }

    private void OnGunUnskilledPenaltyRefresh(Entity<GunUnskilledPenaltyComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (TryGetUserSkills(ent, out var user) &&
            _skills.HasSkill((user, user), ent.Comp.Skill, ent.Comp.Firearms))
        {
            return;
        }

        args.MinAngle += ent.Comp.AngleIncrease;
        args.MaxAngle += ent.Comp.AngleIncrease;
    }

    private void OnGunDamageModifierMapInit(Entity<GunDamageModifierComponent> ent, ref MapInitEvent args)
    {
        RefreshGunDamageMultiplier((ent.Owner, ent.Comp));
    }

    private void OnGunDamageModifierAmmoShot(Entity<GunDamageModifierComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_projectileQuery.TryGetComponent(projectile, out var comp))
                continue;

            comp.Damage *= ent.Comp.ModifiedMultiplier;
        }
    }

    private void OnRecoilSkilledRefreshModifiers(Entity<GunSkilledRecoilComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!TryGetUserSkills(ent, out var user) ||
            !_skills.HasAllSkills((user, user), ent.Comp.Skills))
        {
            return;
        }

        if (ent.Comp.MustBeWielded && CompOrNull<WieldableComponent>(ent)?.Wielded != true)
            return;

        args.CameraRecoilScalar = 0;
    }

    private void OnRequiresSkillsAttemptShoot(Entity<GunRequiresSkillsComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasAllSkills(args.User, ent.Comp.Skills))
            return;

        args.Cancelled = true;

        var popup = Loc.GetString("cm-gun-unskilled", ("gun", ent.Owner));
        _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
    }

    private void OnRequireEquippedAttemptShoot(Entity<GunRequireEquippedComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasRequiredEquippedPopup((ent, ent), args.User))
            return;

        args.Cancelled = true;
    }

    private void StopProjectile(Entity<ProjectileFixedDistanceComponent> projectile)
    {
        if (!_physicsQuery.TryGetComponent(projectile, out var physics))
            return;

        _physics.SetLinearVelocity(projectile, Vector2.Zero, body: physics);
        _physics.SetBodyStatus(projectile, physics, BodyStatus.OnGround);

        if (physics.Awake)
            _broadphase.RegenerateContacts(projectile, physics);
    }

    private void UpdateDelay(Entity<GunShowUseDelayComponent> ent)
    {
        if (!TryComp(ent, out GunComponent? gun))
            return;

        var remaining = gun.NextFire - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
            return;

        var useDelay = EnsureComp<UseDelayComponent>(ent);
        _useDelay.SetLength((ent, useDelay), remaining, ent.Comp.DelayId);
        _useDelay.TryResetDelay((ent, useDelay), false, ent.Comp.DelayId);
    }

    private void TryRefreshGunModifiers<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent?
    {
        if (TryComp(ent, out GunComponent? gun))
            _gun.RefreshModifiers((ent, gun));
    }

    private bool TryGetUserSkills(EntityUid gun, out Entity<SkillsComponent> user)
    {
        user = default;
        if (!TryGetGunUser(gun, out var gunUser) ||
            !TryComp(gunUser, out SkillsComponent? skills))
        {
            return false;
        }

        user = (gunUser, skills);
        return true;
    }

    public void RefreshGunDamageMultiplier(Entity<GunDamageModifierComponent?> gun)
    {
        gun.Comp = EnsureComp<GunDamageModifierComponent>(gun);

        var ev = new GetGunDamageModifierEvent(gun.Comp.Multiplier);
        RaiseLocalEvent(gun, ref ev);

        gun.Comp.ModifiedMultiplier = ev.Multiplier;
    }

    public bool HasRequiredEquippedPopup(Entity<GunRequireEquippedComponent?> gun, EntityUid user)
    {
        if (!Resolve(gun, ref gun.Comp, false))
            return true;

        var slots = _inventory.GetSlotEnumerator(user, SlotFlags.OUTERCLOTHING);
        while (slots.MoveNext(out var slot))
        {
            if (_whitelist.IsValid(gun.Comp.Whitelist, slot.ContainedEntity))
                return true;
        }

        _popup.PopupClient(Loc.GetString("rmc-shoot-harness-required"), user, user, PopupType.MediumCaution);
        return false;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ProjectileFixedDistanceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.FlyEndTime)
                continue;

            StopProjectile((uid, comp));
            RemCompDeferred<ProjectileFixedDistanceComponent>(uid);
            var ev = new ProjectileFixedDistanceStopEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    // RMC revolver cylinder spin default unique action
    private void OnRevolverUniqueAction(Entity<RevolverAmmoProviderComponent> gun, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        int randomCount = _random.Next(1, gun.Comp.Capacity + 1);

        gun.Comp.CurrentIndex = (gun.Comp.CurrentIndex + randomCount) % gun.Comp.Capacity;

        _audio.PlayPredicted(gun.Comp.SoundSpin, gun.Owner, args.UserUid);
        var popup = Loc.GetString("rmc-revolver-spin", ("gun", args.UserUid));
        _popup.PopupClient(popup, args.UserUid, args.UserUid, PopupType.SmallCaution);

        Dirty(gun);
    }

    private void OnUserBlockShootingInsideContainersAttemptShoot(Entity<UserBlockShootingInsideContainersComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        if (_container.IsEntityInContainer(ent))
            args.Cancel();
    }

    private void OnAmmoEjectActivateInWorld(Entity<RMCAmmoEjectComponent> gun, ref ActivateInWorldEvent args)
    {
        if (args.Handled ||
            !_container.TryGetContainer(gun.Owner, gun.Comp.ContainerID, out var container) ||
            container.ContainedEntities.Count <= 0 ||
            !_hands.TryGetActiveHand(args.User, out var hand) ||
            !hand.IsEmpty ||
            !_hands.CanPickupToHand(args.User, container.ContainedEntities[0], hand))
        {
            return;
        }

        var cancelEvent = new RMCTryAmmoEjectEvent(args.User, false);
        RaiseLocalEvent(gun.Owner, ref cancelEvent);

        if (cancelEvent.Cancelled)
            return;

        args.Handled = true;

        var ejectedAmmo = container.ContainedEntities[0];

        // For guns with a BallisticAmmoProviderComponent, if you just remove the ammo from its container, the gun system thinks it's still in the gun and you can still shoot it.
        // So instead I'm having to inflict this shit on our codebase.
        if (TryComp(gun.Owner, out BallisticAmmoProviderComponent? ammoProviderComponent))
        {
            var takeAmmoEvent = new TakeAmmoEvent(1, new List<(EntityUid?, IShootable)>(), Transform(gun.Owner).Coordinates, args.User);
            RaiseLocalEvent(gun.Owner, takeAmmoEvent);

            if (takeAmmoEvent.Ammo.Count <= 0)
                return;

            var ammo = takeAmmoEvent.Ammo[0].Entity;

            if (ammo == null)
                return;

            ejectedAmmo = ammo.Value;
        }

        if (!HasComp<ItemSlotsComponent>(gun.Owner) || !_slots.TryEject(gun.Owner, gun.Comp.ContainerID, args.User, out _, excludeUserAudio: true))
            _audio.PlayPredicted(gun.Comp.EjectSound, gun.Owner, args.User);

        _hands.TryPickup(args.User, ejectedAmmo, hand);
    }

    private void OnGroupSpreadPenaltyEquippedHand(Entity<GunGroupSpreadPenaltyComponent> ent, ref GotEquippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyUnequippedHand(Entity<GunGroupSpreadPenaltyComponent> ent, ref GotUnequippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyRefreshModifiers(Entity<GunGroupSpreadPenaltyComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!TryGetGunUser(ent, out var user))
            return;

        foreach (var hand in user.Comp.Hands)
        {
            if (hand.Value.HeldEntity is not { } held ||
                held == ent.Owner ||
                !_gunGroupSpreadPenalty.HasComp(hand.Value.HeldEntity))
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

    private void OnGroupSpreadPenaltyAmmoShot(Entity<GunGroupSpreadPenaltyComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryGetGunUser(ent, out var user))
            return;

        var other = false;
        foreach (var hand in user.Comp.Hands)
        {
            if (hand.Value.HeldEntity is { } held &&
                held != ent.Owner &&
                _gunGroupSpreadPenalty.HasComp(hand.Value.HeldEntity))
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

    private bool TryGetGunUser(EntityUid gun, out Entity<HandsComponent> user)
    {
        if (_container.TryGetContainingContainer((gun, null), out var container) &&
            TryComp(container.Owner, out HandsComponent? hands))
        {
            user = (container.Owner, hands);
            return true;
        }

        user = default;
        return false;
    }

    private void RefreshGunHolderModifiers(EntityUid gun)
    {
        _gun.RefreshModifiers(gun);
        if (!TryGetGunUser(gun, out var user))
            return;

        foreach (var hand in user.Comp.Hands)
        {
            if (hand.Value.HeldEntity is { } held &&
                held != gun)
            {
                _gun.RefreshModifiers(held);
            }
        }
    }
}
