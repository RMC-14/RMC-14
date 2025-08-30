using System.Numerics;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Evasion;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
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
using Content.Shared.Standing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class CMGunSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetConfigurationManager _netConfig = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectileSystem = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    private readonly int _blockArcCollisionGroup = (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable);

    private const string accuracyExamineColour = "yellow";

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<ShootAtFixedPointComponent, AmmoShotEvent>(OnShootAtFixedPointShot);
        SubscribeLocalEvent<IgnoreArcComponent, BeforeArcEvent>(OnBeforeArc);

        SubscribeLocalEvent<RMCWeaponDamageFalloffComponent, AmmoShotEvent>(OnWeaponDamageFalloffShot);
        SubscribeLocalEvent<RMCWeaponDamageFalloffComponent, GunRefreshModifiersEvent>(OnWeaponDamageFalloffRefreshModifiers);

        SubscribeLocalEvent<RMCExtraProjectilesDamageModsComponent, AmmoShotEvent>(OnExtraProjectilesShot);

        SubscribeLocalEvent<RMCWeaponAccuracyComponent, ExaminedEvent>(OnWeaponAccuracyExamined);
        SubscribeLocalEvent<RMCWeaponAccuracyComponent, GunRefreshModifiersEvent>(OnWeaponAccuracyRefreshModifiers);
        SubscribeLocalEvent<RMCWeaponAccuracyComponent, AmmoShotEvent>(OnWeaponAccuracyShot);

        SubscribeLocalEvent<ProjectileFixedDistanceComponent, PreventCollideEvent>(OnCollisionCheckArc);
        SubscribeLocalEvent<ProjectileFixedDistanceComponent, PhysicsSleepEvent>(OnEventToStopProjectile);

        SubscribeLocalEvent<GunShowUseDelayComponent, GunShotEvent>(OnShowUseDelayShot);
        SubscribeLocalEvent<GunShowUseDelayComponent, ItemWieldedEvent>(OnShowUseDelayWielded);

        SubscribeLocalEvent<GunUserWhitelistComponent, AttemptShootEvent>(OnGunUserWhitelistAttemptShoot);

        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GotEquippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GotUnequippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GunRefreshModifiersEvent>(OnGunUnskilledPenaltyRefresh);
        SubscribeLocalEvent<GunUnskilledPenaltyComponent, GetWeaponAccuracyEvent>(OnGunUnskilledPenaltyGetWeaponAccuracy);

        SubscribeLocalEvent<GunDamageModifierComponent, AmmoShotEvent>(OnGunDamageModifierAmmoShot);
        SubscribeLocalEvent<GunDamageModifierComponent, MapInitEvent>(OnGunDamageModifierMapInit);

        SubscribeLocalEvent<GunPointBlankComponent, AmmoShotEvent>(OnGunPointBlankAmmoShot);

        SubscribeLocalEvent<GunSkilledRecoilComponent, GotEquippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, GotUnequippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, ItemWieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, ItemUnwieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledRecoilComponent, GunRefreshModifiersEvent>(OnRecoilSkilledRefreshModifiers);

        SubscribeLocalEvent<GunSkilledAccuracyComponent, GotEquippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledAccuracyComponent, GotUnequippedHandEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledAccuracyComponent, ItemWieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledAccuracyComponent, ItemUnwieldedEvent>(TryRefreshGunModifiers);
        SubscribeLocalEvent<GunSkilledAccuracyComponent, GetWeaponAccuracyEvent>(OnAccuracySkilledGetWeaponAccuracy);

        SubscribeLocalEvent<GunRequiresSkillsComponent, AttemptShootEvent>(OnRequiresSkillsAttemptShoot);

        SubscribeLocalEvent<GunRequireEquippedComponent, AttemptShootEvent>(OnRequireEquippedAttemptShoot);

        SubscribeLocalEvent<RevolverAmmoProviderComponent, UniqueActionEvent>(OnRevolverUniqueAction);

        SubscribeLocalEvent<UserBlockShootingInsideContainersComponent, ShotAttemptedEvent>(OnUserBlockShootingInsideContainersAttemptShoot);

        SubscribeLocalEvent<RMCAmmoEjectComponent, ActivateInWorldEvent>(OnAmmoEjectActivateInWorld);

        SubscribeLocalEvent<AssistedReloadAmmoComponent, AfterInteractEvent>(OnAssistedReloadAmmoAfterInteract);

        SubscribeLocalEvent<AssistedReloadWeaponComponent, ItemWieldedEvent>(OnAssistedReloadWeaponWielded);
        SubscribeLocalEvent<AssistedReloadWeaponComponent, ItemUnwieldedEvent>(OnAssistedReloadWeaponUnwielded);

        SubscribeLocalEvent<GunDualWieldingComponent, GotEquippedHandEvent>(OnDualWieldingEquippedHand);
        SubscribeLocalEvent<GunDualWieldingComponent, GotUnequippedHandEvent>(OnDualWieldingUnequippedHand);
        SubscribeLocalEvent<GunDualWieldingComponent, GunRefreshModifiersEvent>(OnDualWieldingRefreshModifiers);
        SubscribeLocalEvent<GunDualWieldingComponent, GetWeaponAccuracyEvent>(OnDualWieldingGetWeaponAccuracy);
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

            // Check if the arcing should be disabled.
            var ev = new BeforeArcEvent();
            RaiseLocalEvent(projectile, ref ev);

            // Transfer arcing to the projectile.
            if (Comp<ShootAtFixedPointComponent>(ent).ShootArcProj && !ev.Cancelled)
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
        var ev = new GetDamageFalloffEvent(weapon.Comp.FalloffMultiplier, weapon.Comp.RangeFlat);
        RaiseLocalEvent(weapon.Owner, ref ev);

        weapon.Comp.ModifiedFalloffMultiplier = FixedPoint2.Max(ev.FalloffMultiplier, 0);
        weapon.Comp.RangeFlatModified = ev.Range;

        Dirty(weapon);
    }

    private void OnWeaponDamageFalloffShot(Entity<RMCWeaponDamageFalloffComponent> weapon, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp(projectile, out RMCProjectileDamageFalloffComponent? falloffComponent))
                continue;

            _rmcProjectileSystem.SetProjectileFalloffWeaponMult((projectile, falloffComponent), weapon.Comp.ModifiedFalloffMultiplier, weapon.Comp.RangeFlatModified);
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

    private void OnWeaponAccuracyExamined(Entity<RMCWeaponAccuracyComponent> weapon, ref ExaminedEvent args)
    {
        if (!HasComp<GunComponent>(weapon.Owner))
            return;

        using (args.PushGroup(nameof(RMCWeaponAccuracyComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-examine-text-weapon-accuracy", ("colour", accuracyExamineColour), ("accuracy", weapon.Comp.ModifiedAccuracyMultiplier)));
        }
    }

    private void OnWeaponAccuracyRefreshModifiers(Entity<RMCWeaponAccuracyComponent> weapon, ref GunRefreshModifiersEvent args)
    {
        var baseMult = weapon.Comp.AccuracyMultiplierUnwielded;

        if (TryComp(weapon.Owner, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
            baseMult = weapon.Comp.AccuracyMultiplier;

        var ev = new GetWeaponAccuracyEvent(baseMult, weapon.Comp.RangeFlat);
        RaiseLocalEvent(weapon.Owner, ref ev);

        weapon.Comp.ModifiedAccuracyMultiplier = Math.Max(0.1, (double) ev.AccuracyMultiplier);
        weapon.Comp.RangeFlatModified = ev.Range;

        Dirty(weapon);
    }

    private void OnWeaponAccuracyShot(Entity<RMCWeaponAccuracyComponent> weapon, ref AmmoShotEvent args)
    {
        var netId = GetNetEntity(weapon.Owner).Id;
        FixedPoint2 orderAccuracy = 0;
        FixedPoint2 orderAccuracyPerTile = 0;

        if (TryComp(weapon.Owner, out TransformComponent? transformComponent) &&
            transformComponent.ParentUid.Valid &&
            TryComp(transformComponent.ParentUid, out FocusOrderComponent? orderComponent) &&
            orderComponent.Received.Count != 0)
        {
            orderAccuracy = orderComponent.Received[0].Multiplier * orderComponent.AccuracyModifier;
            orderAccuracyPerTile = orderComponent.Received[0].Multiplier * orderComponent.AccuracyPerTileModifier;
        }

        for (int t = 0; t < args.FiredProjectiles.Count; ++t)
        {
            if (!TryComp(args.FiredProjectiles[t], out RMCProjectileAccuracyComponent? accuracyComponent))
                continue;

            accuracyComponent.Accuracy *= weapon.Comp.ModifiedAccuracyMultiplier;
            accuracyComponent.Accuracy += orderAccuracy;

            var count = 0;
            while (accuracyComponent.Thresholds.Count > count)
            {
                var threshold = accuracyComponent.Thresholds[count];
                accuracyComponent.Thresholds[count] = threshold with { Range = threshold.Range + weapon.Comp.RangeFlatModified };
                count++;
            }

            if (orderAccuracyPerTile != 0)
                accuracyComponent.Thresholds.Add(new AccuracyFalloffThreshold(0f, -orderAccuracyPerTile, false));

            accuracyComponent.GunSeed = (long) t << 32 | netId;
            Dirty<RMCProjectileAccuracyComponent>((args.FiredProjectiles[t], accuracyComponent));
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

    private void OnGunUnskilledPenaltyGetWeaponAccuracy(Entity<GunUnskilledPenaltyComponent> ent, ref GetWeaponAccuracyEvent args)
    {
        if (TryGetUserSkills(ent, out var user) &&
            _skills.HasSkill((user, user), ent.Comp.Skill, ent.Comp.Firearms))
        {
            return;
        }

        args.AccuracyMultiplier += ent.Comp.AccuracyAddMult;
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

    private void OnGunPointBlankMeleeHit(Entity<GunPointBlankComponent> gun, ref MeleeHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(gun, out var melee))
            return;

        if (!TryGetGunUser(gun.Owner, out var user))
            return;

        var userDelay = EnsureComp<UserPointblankCooldownComponent>(user);

        //After meleeing can't PB
        userDelay.LastPBAt = _timing.CurTime;
    }

    private void OnGunPointBlankAmmoShot(Entity<GunPointBlankComponent> gun, ref AmmoShotEvent args)
    {
        if (!TryComp(gun.Owner, out GunComponent? gunComp) ||
            gunComp.Target == null ||
            !HasComp<TransformComponent>(gunComp.Target) ||
            !HasComp<EvasionComponent>(gunComp.Target))
        {
            return;
        }

        if (!TryGetGunUser(gun.Owner, out var user))
            return;

        var userDelay = EnsureComp<UserPointblankCooldownComponent>(user);
        if (_timing.CurTime < userDelay.LastPBAt + userDelay.TimeBetweenPBs)
            return;

        if (gunComp.Target.Value == user.Owner)
        {
            if (gunComp.SelectedMode == SelectiveFire.FullAuto)
                return;

            if (TryComp(user, out ActorComponent? actor) &&
                !_netConfig.GetClientCVar(actor.PlayerSession.Channel, RMCCVars.RMCDamageYourself))
            {
                return;
            }
        }

        if (!_interaction.InRangeUnobstructed(gun.Owner, gunComp.Target.Value, gun.Comp.Range))
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp(projectile, out ProjectileComponent? projectileComp) ||
                !TryComp(projectile, out PhysicsComponent? physicsComp) ||
                gun.Comp.Range < (_transform.GetMoverCoordinates(gunComp.Target.Value).Position - _transform.GetMoverCoordinates(projectile).Position).Length())
            {
                continue;
            }

            if (_standing.IsDown(gunComp.Target.Value))
            {
                projectileComp.Damage *= gun.Comp.ProneDamageMult;
                Dirty(projectile, projectileComp);
            }

            _projectile.ProjectileCollide((projectile, projectileComp, physicsComp), gunComp.Target.Value);
        }

        userDelay.LastPBAt = _timing.CurTime;

        if (!TryComp<MeleeWeaponComponent>(gun, out var melee))
            return;

        //Can't melee right after a PB
        melee.NextAttack = userDelay.LastPBAt + userDelay.TimeBetweenPBs;
        Dirty(gun, melee);
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

    private void OnAccuracySkilledGetWeaponAccuracy(Entity<GunSkilledAccuracyComponent> gun, ref GetWeaponAccuracyEvent args)
    {
        if (!TryGetUserSkills(gun, out var user))
            return;

        args.AccuracyMultiplier += gun.Comp.AccuracyAddMult * _skills.GetSkill((user, user), gun.Comp.Skill);
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
            _hands.GetActiveHand(args.User) is not { } hand ||
            !_hands.HandIsEmpty(args.User, hand) ||
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

    private void OnDualWieldingEquippedHand(Entity<GunDualWieldingComponent> gun, ref GotEquippedHandEvent args)
    {
        RefreshGunHolderModifiers(gun, args.User);
    }

    private void OnDualWieldingUnequippedHand(Entity<GunDualWieldingComponent> gun, ref GotUnequippedHandEvent args)
    {
        RefreshGunHolderModifiers(gun, args.User);
    }

    private void OnDualWieldingRefreshModifiers(Entity<GunDualWieldingComponent> gun, ref GunRefreshModifiersEvent args)
    {
        if (gun.Comp.WeaponGroup == GunDualWieldingGroup.None || !TryGetGunUser(gun, out var user))
            return;

        if (!TryGetOtherDualWieldedGun(user, gun, out _))
            return;

        args.CameraRecoilScalar += gun.Comp.RecoilModifier;
        args.MinAngle += gun.Comp.ScatterModifier;
        args.MaxAngle += gun.Comp.ScatterModifier;
    }

    private void OnDualWieldingGetWeaponAccuracy(Entity<GunDualWieldingComponent> gun, ref GetWeaponAccuracyEvent args)
    {
        if (gun.Comp.WeaponGroup == GunDualWieldingGroup.None || !TryGetGunUser(gun, out var user))
            return;

        if (!TryGetOtherDualWieldedGun(user, gun, out _))
            return;

        args.AccuracyMultiplier += gun.Comp.AccuracyAddMult;
    }

    private bool TryGetOtherDualWieldedGun(EntityUid user, Entity<GunDualWieldingComponent> gun, out Entity<GunDualWieldingComponent> otherGun)
    {
        otherGun = default;

        if (!TryComp(user, out HandsComponent? handsComp))
            return false;

        foreach (var hand in handsComp.Hands.Keys)
        {
            if (_hands.GetHeldItem(user, hand) is { } held &&
                held != gun.Owner &&
                TryComp(held, out GunDualWieldingComponent? dualWieldingComp) &&
                dualWieldingComp.WeaponGroup == gun.Comp.WeaponGroup)
            {
                otherGun = (held, dualWieldingComp);
                return true;
            }
        }

        return false;
    }

    public bool TryGetGunUser(EntityUid gun, out Entity<HandsComponent> user)
    {
        if (_container.TryGetContainingContainer((gun, null), out var container) &&
            TryComp(container.Owner, out HandsComponent? hands))
        {
            user = (container.Owner, hands);
            return true;
        }

        if (container != null && TryComp(container.Owner, out AttachableHolderComponent? holder) && holder.SupercedingAttachable == gun)
            return TryGetGunUser(container.Owner, out user);

        user = default;
        return false;
    }

    private void RefreshGunHolderModifiers(Entity<GunDualWieldingComponent> gun, EntityUid user)
    {
        _gun.RefreshModifiers(gun.Owner);
        if (!TryGetOtherDualWieldedGun(user, gun, out var otherGun))
            return;

        _gun.RefreshModifiers(otherGun.Owner);
    }

    private void OnAssistedReloadAmmoAfterInteract(Entity<AssistedReloadAmmoComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        TryAssistedReload(args.User, args.Target.Value, ent);
    }

    private bool IsBehindTarget(EntityUid user, EntityUid target)
    {
        var targetFacingDirection = Transform(target).LocalRotation.GetCardinalDir();
        var behindAngle = targetFacingDirection.GetOpposite().ToAngle();

        var userMapPos = _transform.GetMapCoordinates(user);
        var targetMapPos = _transform.GetMapCoordinates(target);
        var currentAngle = (userMapPos.Position - targetMapPos.Position).ToWorldAngle();

        var differenceFromBehindAngle = (behindAngle.Degrees - currentAngle.Degrees + 180 + 360) % 360 - 180;

        if (differenceFromBehindAngle > -45 && differenceFromBehindAngle < 45)
            return true;

        return false;
    }

    private void TryAssistedReload(EntityUid user, EntityUid target, Entity<AssistedReloadAmmoComponent> ammo)
    {
        if (!TryComp<AssistedReloadReceiverComponent>(target, out var reloadReceiver))
            return;

        if (reloadReceiver.Weapon == null)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(reloadReceiver.Weapon, out var ballisticAmmoProvider))
            return;

        if (_whitelist.IsWhitelistFailOrNull(ballisticAmmoProvider.Whitelist, ammo.Owner))
        {
            var failMismatchPopup = Loc.GetString("rmc-assisted-reload-fail-mismatch", ("ammo", ammo.Owner), ("weapon", reloadReceiver.Weapon));
            _popup.PopupClient(failMismatchPopup, user, user, PopupType.SmallCaution);
            return;
        }

        if (!IsBehindTarget(user, target))
        {
            var failAnglePopup = Loc.GetString("rmc-assisted-reload-fail-angle", ("target", target));
            _popup.PopupClient(failAnglePopup, user, user, PopupType.SmallCaution);
            return;
        }

        if (!_gun.TryAmmoInsert(reloadReceiver.Weapon.Value, ballisticAmmoProvider, ammo.Owner, user, reloadReceiver.Weapon.Value, ammo.Comp.InsertDelay))
        {
            var failFullPopup = Loc.GetString("rmc-assisted-reload-fail-full", ("target", target), ("weapon", reloadReceiver.Weapon));
            _popup.PopupClient(failFullPopup, user, user, PopupType.SmallCaution);
            return;
        }

        var userPopup = Loc.GetString("rmc-assisted-reload-start-user", ("target", target), ("weapon", reloadReceiver.Weapon));
        var targetPopup = Loc.GetString("rmc-assisted-reload-start-target", ("reloader", user), ("weapon", reloadReceiver.Weapon), ("ammo", ammo.Owner));

        _popup.PopupClient(userPopup, user, user);
        _popup.PopupEntity(targetPopup, target, target);
    }

    private void OnAssistedReloadWeaponWielded(Entity<AssistedReloadWeaponComponent> ent, ref ItemWieldedEvent args)
    {
        if (!TryGetGunUser(ent.Owner, out var wielder))
            return;

        var receiver = EnsureComp<AssistedReloadReceiverComponent>(wielder);
        receiver.Weapon = ent.Owner;
    }

    private void OnAssistedReloadWeaponUnwielded(Entity<AssistedReloadWeaponComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (!TryGetGunUser(ent.Owner, out var wielder))
            return;

        RemCompDeferred<AssistedReloadReceiverComponent>(wielder);
    }

    // Do not arc the projectile if it has the IgnoreArcComponent
    private void OnBeforeArc(Entity<IgnoreArcComponent> ent, ref BeforeArcEvent args)
    {
        args.Cancelled = true;
    }
}

/// <summary>
/// DoAfter event for filling a ballistic ammo provider directly while InsertDelay > 0.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DelayedAmmoInsertDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// DoAfter event for cycling a ballistic ammo provider while CycleDelay > 0.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DelayedCycleDoAfterEvent : SimpleDoAfterEvent;
