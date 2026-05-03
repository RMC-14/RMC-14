using System.Numerics;
using Content.Server._RMC14.Damage;
using Content.Server._RMC14.Targeting;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Projectiles.Penetration;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Weapons.Ranged.Brute;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Weapons.Ranged.Brute;

public sealed class RMCBruteLauncherSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly LocId InvalidTarget = "rmc-brute-launcher-invalid-target";
    private static readonly LocId LockInterrupted = "rmc-brute-launcher-lock-interrupted";
    private static readonly LocId NoAmmo = "gun-magazine-fired-empty";
    private static readonly LocId RequiresWield = "rmc-brute-launcher-requires-wield";
    private static readonly LocId TargetObscured = "rmc-brute-launcher-target-obscured";
    private static readonly LocId Unskilled = "cm-gun-unskilled";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly RMCTargetingSystem _targeting = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private sealed class BruteWaveState
    {
        public BruteWaveState(int structureDamage)
        {
            StructureDamage = structureDamage;
        }

        public int StructureDamage;
    }

    private readonly struct BruteWaveTile
    {
        public BruteWaveTile(Vector2i offset, bool edge)
        {
            Offset = offset;
            Edge = edge;
        }

        public readonly Vector2i Offset;
        public readonly bool Edge;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBruteLauncherComponent, AttemptShootEvent>(OnAttemptShoot, before: [typeof(CMGunSystem)]);
        SubscribeLocalEvent<RMCBruteLauncherComponent, RMCBruteLockOnDoAfterEvent>(OnLockOnDoAfter);
        SubscribeLocalEvent<RMCBruteLauncherComponent, DoAfterAttemptEvent<RMCBruteLockOnDoAfterEvent>>(OnLockOnDoAfterAttempt);
        SubscribeLocalEvent<RMCBruteLauncherComponent, ComponentShutdown>(OnLauncherShutdown);

        SubscribeLocalEvent<RMCBruteProjectileComponent, AfterProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<RMCBruteProjectileComponent, ProjectileFixedDistanceStopEvent>(OnProjectileFixedDistanceStop);

        SubscribeLocalEvent<RMCProjectileSkipXenosComponent, PreventCollideEvent>(OnSkipXenosPreventCollide);
        SubscribeLocalEvent<RMCBackblastOnShootComponent, GunShotEvent>(OnBackblastShot);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCBruteProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out _, out var brute, out var physics))
        {
            if (physics.LinearVelocity.LengthSquared() > 0.01f)
                brute.LastDirection = Vector2.Normalize(physics.LinearVelocity);
        }
    }

    private void OnAttemptShoot(Entity<RMCBruteLauncherComponent> launcher, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        // Completed lock-on calls back into the gun system; let it pass without starting another lock-on.
        if (launcher.Comp.LockComplete)
            return;

        if (launcher.Comp.LockTarget != null)
        {
            args.Cancelled = true;
            return;
        }

        // Leave empty fire handling to the gun system so click feedback matches other guns.
        if (!HasAmmo(launcher.Owner))
            return;

        args.Cancelled = true;

        if (!IsWielded(launcher.Owner))
        {
            args.Message = Loc.GetString(RequiresWield);
            return;
        }

        if (!_skills.HasSkill(args.User, launcher.Comp.RequiredSkill, launcher.Comp.RequiredSkillLevel))
        {
            args.Message = Loc.GetString(Unskilled, ("gun", launcher.Owner));
            return;
        }

        if (!TryComp(launcher, out GunComponent? gun) ||
            args.ToCoordinates is not { } targetCoordinates ||
            !TryGetTarget(gun, targetCoordinates, out var target))
        {
            args.Message = Loc.GetString(InvalidTarget);
            return;
        }

        if (!TryValidateTarget(args.User, target, out var messageId))
        {
            args.Message = Loc.GetString(messageId);
            return;
        }

        StartLockOn(launcher, args.User, target, targetCoordinates);
    }

    private void OnLockOnDoAfter(Entity<RMCBruteLauncherComponent> launcher, ref RMCBruteLockOnDoAfterEvent args)
    {
        if (args.LockId != launcher.Comp.LockId)
            return;

        CleanupTargeting(launcher);

        if (args.Handled || args.Cancelled)
        {
            if (!TerminatingOrDeleted(args.User))
                _popup.PopupEntity(Loc.GetString(LockInterrupted), args.User, args.User, PopupType.SmallCaution);

            return;
        }

        args.Handled = true;

        if (!TryComp(launcher, out GunComponent? gun))
            return;

        if (!IsWielded(launcher.Owner))
        {
            _popup.PopupEntity(Loc.GetString(RequiresWield), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!HasAmmo(launcher.Owner))
        {
            _popup.PopupEntity(Loc.GetString(NoAmmo), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var target = GetEntity(args.Target);
        if (TerminatingOrDeleted(target))
            return;

        if (!TryValidateTarget(args.User, target, out var messageId))
        {
            _popup.PopupEntity(Loc.GetString(messageId), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var coordinates = GetCoordinates(args.Coordinates);
        launcher.Comp.LockComplete = true;
        Dirty(launcher);

        try
        {
            _gun.AttemptShoot(args.User, launcher.Owner, gun, coordinates);
        }
        finally
        {
            launcher.Comp.LockComplete = false;
            Dirty(launcher);
        }
    }

    private void OnLockOnDoAfterAttempt(Entity<RMCBruteLauncherComponent> launcher, ref DoAfterAttemptEvent<RMCBruteLockOnDoAfterEvent> args)
    {
        var target = GetEntity(args.Event.Target);
        if (args.Event.LockId != launcher.Comp.LockId ||
            launcher.Comp.LockTarget != target ||
            TerminatingOrDeleted(target) ||
            !IsBruteTarget(target) ||
            !IsWielded(launcher.Owner) ||
            !HasAmmo(launcher.Owner))
        {
            args.Cancel();
        }
    }

    private void OnLauncherShutdown(Entity<RMCBruteLauncherComponent> launcher, ref ComponentShutdown args)
    {
        CleanupTargeting(launcher);
    }

    private void StartLockOn(Entity<RMCBruteLauncherComponent> launcher, EntityUid user, EntityUid target, EntityCoordinates targetCoordinates)
    {
        if (launcher.Comp.LockTarget != null)
            return;

        CleanupTargeting(launcher);

        // A monotonic id keeps stale do-afters from completing after a newer lock attempt started.
        var lockId = ++launcher.Comp.LockId;
        launcher.Comp.LockTarget = target;
        Dirty(launcher);

        _targeting.Target(launcher.Owner, user, target, (float) launcher.Comp.AimDelay.TotalSeconds, launcher.Comp.TargetedEffect, launcher.Comp.ShowDirection);
        if (TryComp(target, out RMCTargetedComponent? targeted))
        {
            targeted.LockOnState = launcher.Comp.LockOnState;
            targeted.LockOnStateDirection = launcher.Comp.LockOnStateDirection;
            Dirty(target, targeted);
        }

        if (TryComp(launcher, out TargetingLaserComponent? laser))
        {
            laser.LaserState = launcher.Comp.LaserState;
            Dirty(launcher.Owner, laser);
        }

        var ev = new RMCBruteLockOnDoAfterEvent(lockId, GetNetEntity(target), GetNetCoordinates(targetCoordinates));
        var doAfter = new DoAfterArgs(EntityManager, user, launcher.Comp.AimDelay, ev, launcher.Owner, used: launcher.Owner)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = true,
            RequireCanInteract = false,
            RangeCheck = false,
            // Re-check ammo, wield state, and target lifetime continuously while the guided lock is visible.
            AttemptFrequency = AttemptFrequency.EveryTick,
            ForceVisible = true,
            CancelDuplicate = false,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            CleanupTargeting(launcher);
    }

    private void CleanupTargeting(Entity<RMCBruteLauncherComponent> launcher)
    {
        if (launcher.Comp.LockTarget is not { } target)
            return;

        launcher.Comp.LockTarget = null;
        Dirty(launcher);

        if (!TerminatingOrDeleted(target))
            _targeting.StopTargeting((launcher.Owner, null), target);
    }

    private bool TryGetTarget(GunComponent gun, EntityCoordinates coordinates, out EntityUid target)
    {
        // Fixed-point guns may resolve a clicked entity; otherwise look around the clicked tile.
        if (gun.Target is { } gunTarget && IsBruteTarget(gunTarget))
        {
            target = gunTarget;
            return true;
        }

        var mapCoordinates = _transform.ToMapCoordinates(coordinates);
        foreach (var ent in _lookup.GetEntitiesInRange<TransformComponent>(mapCoordinates, 0.45f))
        {
            if (!IsBruteTarget(ent.Owner))
                continue;

            target = ent.Owner;
            return true;
        }

        target = default;
        return false;
    }

    private bool TryValidateTarget(EntityUid user, EntityUid target, out LocId messageId)
    {
        if (!IsBruteTarget(target))
        {
            messageId = InvalidTarget;
            return false;
        }

        if (IsTargetObscured(user, target))
        {
            messageId = TargetObscured;
            return false;
        }

        messageId = default;
        return true;
    }

    private bool IsBruteTarget(EntityUid target)
    {
        if (TerminatingOrDeleted(target) ||
            HasComp<MobStateComponent>(target) ||
            HasComp<XenoComponent>(target))
        {
            return false;
        }

        if (!HasComp<TransformComponent>(target))
            return false;

        return _tag.HasAnyTag(target, StructureTag, WallTag) &&
               CanBruteBreakStructure(target);
    }

    private bool CanBruteBreakStructure(EntityUid target)
    {
        if (HasComp<RMCWallExplosionDeletableComponent>(target))
            return true;

        return HasComp<DamageableComponent>(target) &&
               _rmcDamageable.TryGetDestroyedAt(target, out var destroyedAt) &&
               destroyedAt is { } threshold &&
               threshold < FixedPoint2.MaxValue;
    }

    private bool IsTargetObscured(EntityUid user, EntityUid target)
    {
        var userMap = _transform.GetMapCoordinates(user);
        var targetMap = _transform.GetMapCoordinates(target);
        if (userMap.MapId != targetMap.MapId)
            return true;

        if (!_mapManager.TryFindGridAt(userMap, out var userGrid, out var grid) ||
            !_mapManager.TryFindGridAt(targetMap, out var targetGrid, out _) ||
            userGrid != targetGrid)
        {
            return !_examine.InRangeUnOccluded(userMap, targetMap, 0, uid => uid == user || uid == target);
        }

        var start = _map.WorldToTile(userGrid, grid, userMap.Position);
        var end = _map.WorldToTile(userGrid, grid, targetMap.Position);
        var targetIsWall = _tag.HasTag(target, WallTag);

        // The target wall can be opaque, but intermediate opaque walls should block lock-on.
        foreach (var tile in GetLine(start, end))
        {
            if (tile == start || (tile == end && targetIsWall))
                continue;

            var anchored = _map.GetAnchoredEntitiesEnumerator(userGrid, grid, tile);
            while (anchored.MoveNext(out var uid))
            {
                if (uid == null || uid.Value == user || uid.Value == target)
                    continue;

                if (IsOpaqueWall(uid.Value))
                    return true;
            }
        }

        return false;
    }

    private bool IsOpaqueWall(EntityUid target)
    {
        return _tag.HasTag(target, WallTag) &&
               TryComp(target, out OccluderComponent? occluder) &&
               occluder.Enabled;
    }

    private static IEnumerable<Vector2i> GetLine(Vector2i start, Vector2i end)
    {
        var x = start.X;
        var y = start.Y;
        var dx = Math.Abs(end.X - start.X);
        var dy = Math.Abs(end.Y - start.Y);
        var sx = start.X < end.X ? 1 : -1;
        var sy = start.Y < end.Y ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            yield return new Vector2i(x, y);

            if (x == end.X && y == end.Y)
                yield break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private bool IsWielded(EntityUid launcher)
    {
        return !TryComp(launcher, out WieldableComponent? wieldable) || wieldable.Wielded;
    }

    private bool HasAmmo(EntityUid launcher)
    {
        var ev = new GetAmmoCountEvent();
        RaiseLocalEvent(launcher, ref ev);
        return ev.Count > 0;
    }

    private void OnProjectileHit(Entity<RMCBruteProjectileComponent> projectile, ref AfterProjectileHitEvent args)
    {
        Prime(projectile, _transform.GetMapCoordinates(args.Target));
    }

    private void OnProjectileFixedDistanceStop(Entity<RMCBruteProjectileComponent> projectile, ref ProjectileFixedDistanceStopEvent args)
    {
        Prime(projectile);
    }

    private void Prime(Entity<RMCBruteProjectileComponent> projectile, MapCoordinates? targetCoordinates = null)
    {
        if (projectile.Comp.Primed)
            return;

        projectile.Comp.Primed = true;

        var direction = GetWaveDirection(projectile.Comp.LastDirection);
        var origin = targetCoordinates ?? _transform.GetMapCoordinates(projectile.Owner);
        var centered = new MapCoordinates(
            new Vector2(MathF.Floor(origin.Position.X) + 0.5f, MathF.Floor(origin.Position.Y) + 0.5f),
            origin.MapId);

        // On impact the breach wave starts one tile before the hit turf, then walks through the target.
        if (targetCoordinates is { })
        {
            var impactOffset = GetAngleTargetOffset(direction, 1);
            centered = centered.Offset(new Vector2(-impactOffset.X, -impactOffset.Y));
        }

        var seen = new HashSet<Vector2i> { Vector2i.Zero };
        var wave = new BruteWaveState(projectile.Comp.StructureDamage);

        // Spawn each row separately to keep the cascading 0.1 second breach wave.
        for (var i = 0; i <= projectile.Comp.MaxDistance; i++)
        {
            var row = i;
            var tiles = GetWaveRow(projectile.Comp, direction, row);
            Timer.Spawn(TimeSpan.FromSeconds(projectile.Comp.RowDelay * row), () => DetonateRow(projectile.Comp, centered, tiles, seen, wave));
        }

        QueueDel(projectile.Owner);
    }

    private void DetonateRow(
        RMCBruteProjectileComponent component,
        MapCoordinates origin,
        List<BruteWaveTile> tiles,
        HashSet<Vector2i> seen,
        BruteWaveState wave)
    {
        foreach (var tile in tiles)
        {
            DetonateTile(component, origin, tile.Offset, tile.Edge, seen, wave);
        }
    }

    private void DetonateTile(
        RMCBruteProjectileComponent component,
        MapCoordinates origin,
        Vector2i offset,
        bool edge,
        HashSet<Vector2i> seen,
        BruteWaveState wave)
    {
        if (!seen.Add(offset))
            return;

        var coordinates = new MapCoordinates(origin.Position + new Vector2(offset.X, offset.Y), origin.MapId);
        // Keep edge damage wave-local so rolls cannot leak between shots.
        if (edge)
            wave.StructureDamage = _random.Next(component.EdgeLowerDamage, component.EdgeUpperDamage + 1);

        var damageAmount = wave.StructureDamage;
        foreach (var damageable in _lookup.GetEntitiesInRange<DamageableComponent>(coordinates, 0.45f))
        {
            if (!IsWaveStructure(damageable.Owner))
                continue;

            var effectiveDamage = GetStructureDamage(component, damageable.Owner, damageAmount);
            var damage = new DamageSpecifier
            {
                DamageDict =
                {
                    ["Structural"] = effectiveDamage,
                },
            };

            _damageable.TryChangeDamage(damageable.Owner, damage, true, damageable: damageable.Comp);
        }

        // Some wall-like entities are modeled as explosion-deletable rather than damageable in RMC.
        foreach (var wall in _lookup.GetEntitiesInRange<RMCWallExplosionDeletableComponent>(coordinates, 0.45f))
        {
            if (IsWaveStructure(wall.Owner) && !HasComp<DamageableComponent>(wall.Owner))
                QueueDel(wall.Owner);
        }

        var spawnCoordinates = _transform.ToCoordinates(coordinates);

        // Fire, sparks, and smoke are independent rolls, not one shared effect roll.
        if (_random.Prob(component.FireChance))
            SpawnAtPosition(component.FirePrototype, spawnCoordinates);

        if (_random.Prob(component.SparkChance))
            SpawnAtPosition(component.SparkPrototype, spawnCoordinates);

        if (_random.Prob(component.SmokeChance))
            SpawnAtPosition(component.SmokePrototype, spawnCoordinates);

        ThrowEntities(component, origin, coordinates, offset);
    }

    private bool IsWaveStructure(EntityUid target)
    {
        return !TerminatingOrDeleted(target) &&
               _tag.HasAnyTag(target, StructureTag, WallTag);
    }

    private float GetStructureDamage(RMCBruteProjectileComponent component, EntityUid target, int damageAmount)
    {
        var damage = (float) damageAmount;
        // Scale RMC walls and doors separately so BRUTE breakage stays consistent.
        if (_tag.HasTag(target, WallTag))
        {
            damage *= component.WallDamageMultiplier;
        }
        else if (TryComp(target, out DoorComponent? door))
        {
            damage *= component.DoorDamageMultiplier;
            if (IsDoorOpenForExplosion(door))
                damage *= component.OpenDoorDamageMultiplier;
        }
        else if (TryComp(target, out RMCBruteStructureDamageMultiplierComponent? bruteMultiplier))
        {
            damage *= bruteMultiplier.Multiplier;
        }

        if (HasComp<XenoConstructComponent>(target))
            damage *= component.ResinExplosionDamageMultiplier;

        return damage;
    }

    private static bool IsDoorOpenForExplosion(DoorComponent door)
    {
        return door.State == DoorState.Open ||
               door.State == DoorState.Opening && door.Partial ||
               door.State == DoorState.Closing && !door.Partial;
    }

    private void ThrowEntities(RMCBruteProjectileComponent component, MapCoordinates origin, MapCoordinates coordinates, Vector2i offset)
    {
        if (offset == Vector2i.Zero)
            return;

        foreach (var ent in _lookup.GetEntitiesInRange<TransformComponent>(coordinates, 0.45f))
        {
            if (ent.Comp.Anchored ||
                IsWaveStructure(ent.Owner))
            {
                continue;
            }

            var skipChance = component.ThrowSkipChance;
            if (HasComp<MobStateComponent>(ent.Owner))
            {
                // Larger living targets are harder to throw, stored here as an added skip chance.
                var size = _size.TryGetSize(ent.Owner, out var foundSize)
                    ? (int) foundSize
                    : 1;

                skipChance += size * component.LivingSizeSkipChance;
            }

            if (_random.Prob(Math.Clamp(skipChance, 0f, 1f)))
                continue;

            var entityCoordinates = _transform.GetMapCoordinates(ent.Owner);
            var direction = entityCoordinates.Position - origin.Position;
            if (direction.LengthSquared() <= 0.01f)
                continue;

            _throwing.TryThrow(ent.Owner, Vector2.Normalize(direction), component.ThrowSpeed, animated: false, playSound: false, compensateFriction: true);
        }
    }

    private void OnSkipXenosPreventCollide(Entity<RMCProjectileSkipXenosComponent> projectile, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<XenoComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnBackblastShot(Entity<RMCBackblastOnShootComponent> launcher, ref GunShotEvent args)
    {
        var fromMap = _transform.ToMapCoordinates(args.FromCoordinates);
        var toMap = _transform.ToMapCoordinates(args.ToCoordinates);
        if (fromMap.MapId != toMap.MapId)
            return;

        var shotDirection = toMap.Position - fromMap.Position;
        if (shotDirection.LengthSquared() <= 0.01f)
            return;

        var back = fromMap.Offset(shotDirection.GetDir().GetOpposite().ToVec());
        var backCoordinates = _transform.ToCoordinates(back);
        SpawnAtPosition(launcher.Comp.SmokePrototype, backCoordinates);
        _audio.PlayPvs(launcher.Comp.Sound, backCoordinates);

        ApplyShooterBackblast(args.User, launcher.Comp);

        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(back, launcher.Comp.TileRange))
        {
            if (_standing.IsDown(mob.Owner) ||
                _deafness.HasEarProtection(mob.Owner))
            {
                continue;
            }

            _damageable.TryChangeDamage(mob.Owner, new DamageSpecifier(launcher.Comp.Damage), origin: args.User, tool: launcher.Owner);
            _stun.TryParalyze(mob.Owner, launcher.Comp.KnockdownTime, true);
            _dazed.TryDaze(mob.Owner, launcher.Comp.StutterTime, true, stutter: true);
            _deafness.TryDeafen(mob.Owner, launcher.Comp.DeafTime, true);
        }
    }

    private void ApplyShooterBackblast(EntityUid user, RMCBackblastOnShootComponent component)
    {
        if (_deafness.HasEarProtection(user))
            return;

        _dazed.TryDaze(user, component.StutterTime, true, stutter: true);
        _deafness.TryDeafen(user, component.DeafTime, true);
    }

    private static List<BruteWaveTile> GetWaveRow(RMCBruteProjectileComponent component, Vector2 direction, int row)
    {
        var tiles = new List<BruteWaveTile>();
        var center = GetAngleTargetOffset(direction, row);
        tiles.Add(new BruteWaveTile(center, false));

        var right = RotateDirection(direction, MathF.PI / 2);
        var left = RotateDirection(direction, -MathF.PI / 2);
        var diagonalRight = RotateDirection(direction, MathF.PI * 3 / 4);
        var diagonalLeft = RotateDirection(direction, -MathF.PI * 3 / 4);

        // Keep narrow near/far rows and wide middle rows; diagonals only join after row two.
        var maxWidth = row == 1 || row == component.MaxDistance ? 1 : 2;
        for (var width = 1; width <= maxWidth; width++)
        {
            var edge = width == maxWidth;

            tiles.Add(new BruteWaveTile(center + GetAngleTargetOffset(right, width), false));
            tiles.Add(new BruteWaveTile(center + GetAngleTargetOffset(left, width), false));

            if (row <= 2)
                continue;

            tiles.Add(new BruteWaveTile(center + GetAngleTargetOffset(diagonalRight, width), edge));
            tiles.Add(new BruteWaveTile(center + GetAngleTargetOffset(diagonalLeft, width), edge));
        }

        return tiles;
    }

    private static Vector2 GetWaveDirection(Vector2 direction)
    {
        return direction.LengthSquared() <= 0.01f
            ? Vector2.UnitX
            : Vector2.Normalize(direction);
    }

    private static Vector2i GetAngleTargetOffset(Vector2 direction, int range)
    {
        return new Vector2i(
            RoundToTile(direction.X * range),
            RoundToTile(direction.Y * range));
    }

    private static Vector2 RotateDirection(Vector2 direction, float radians)
    {
        var sin = MathF.Sin(radians);
        var cos = MathF.Cos(radians);
        return new Vector2(
            direction.X * cos + direction.Y * sin,
            direction.Y * cos - direction.X * sin);
    }

    private static int RoundToTile(float value)
    {
        return (int) MathF.Round(value, MidpointRounding.AwayFromZero);
    }
}
