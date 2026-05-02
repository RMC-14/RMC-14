using System.Numerics;
using Content.Server._RMC14.Targeting;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Projectiles.Penetration;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Weapons.Ranged.Brute;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
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

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly RMCTargetingSystem _targeting = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBruteLauncherComponent, AttemptShootEvent>(OnAttemptShoot, before: [typeof(CMGunSystem)]);
        SubscribeLocalEvent<RMCBruteLauncherComponent, RMCBruteLockOnDoAfterEvent>(OnLockOnDoAfter);
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

        if (launcher.Comp.LockComplete)
            return;

        args.Cancelled = true;
        args.ResetCooldown = true;

        if (!_skills.HasSkill(args.User, launcher.Comp.RequiredSkill, launcher.Comp.RequiredSkillLevel))
        {
            args.Message = Loc.GetString("cm-gun-unskilled", ("gun", launcher.Owner));
            return;
        }

        if (!TryComp(launcher, out GunComponent? gun) ||
            args.ToCoordinates is not { } targetCoordinates ||
            !TryGetTarget(gun, targetCoordinates, out var target))
        {
            args.Message = Loc.GetString("rmc-brute-launcher-invalid-target");
            return;
        }

        if (!TryValidateTarget(args.User, target, launcher.Comp.MaxRange, out var message))
        {
            args.Message = message;
            return;
        }

        StartLockOn(launcher, args.User, target, targetCoordinates);
    }

    private void OnLockOnDoAfter(Entity<RMCBruteLauncherComponent> launcher, ref RMCBruteLockOnDoAfterEvent args)
    {
        CleanupTargeting(launcher);

        if (args.Handled || args.Cancelled)
        {
            if (!TerminatingOrDeleted(args.User))
                _popup.PopupEntity(Loc.GetString("rmc-brute-launcher-lock-interrupted"), args.User, args.User, PopupType.SmallCaution);

            return;
        }

        args.Handled = true;

        if (!TryComp(launcher, out GunComponent? gun))
            return;

        var target = GetEntity(args.Target);
        if (TerminatingOrDeleted(target))
            return;

        if (!TryValidateTarget(args.User, target, launcher.Comp.MaxRange, out var message))
        {
            _popup.PopupEntity(message, args.User, args.User, PopupType.SmallCaution);
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

    private void OnLauncherShutdown(Entity<RMCBruteLauncherComponent> launcher, ref ComponentShutdown args)
    {
        CleanupTargeting(launcher);
    }

    private void StartLockOn(Entity<RMCBruteLauncherComponent> launcher, EntityUid user, EntityUid target, EntityCoordinates targetCoordinates)
    {
        CleanupTargeting(launcher);

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

        var ev = new RMCBruteLockOnDoAfterEvent(GetNetEntity(target), GetNetCoordinates(targetCoordinates));
        var doAfter = new DoAfterArgs(EntityManager, user, launcher.Comp.AimDelay, ev, launcher.Owner, target: target, used: launcher.Owner)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = true,
            RequireCanInteract = false,
            RangeCheck = false,
            ForceVisible = true,
            CancelDuplicate = true,
            BlockDuplicate = true,
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

    private bool TryValidateTarget(EntityUid user, EntityUid target, float maxRange, out string message)
    {
        if (!IsBruteTarget(target))
        {
            message = Loc.GetString("rmc-brute-launcher-invalid-target");
            return false;
        }

        var userMap = _transform.GetMapCoordinates(user);
        var targetMap = _transform.GetMapCoordinates(target);
        if (userMap.MapId != targetMap.MapId ||
            (targetMap.Position - userMap.Position).Length() > maxRange + 0.01f)
        {
            message = Loc.GetString("rmc-brute-launcher-out-of-range");
            return false;
        }

        if (!_examine.InRangeUnOccluded(user, target, maxRange, uid => uid == user || uid == target))
        {
            message = Loc.GetString("rmc-brute-launcher-target-obscured");
            return false;
        }

        message = string.Empty;
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

        if (!TryComp(target, out TransformComponent? transform) || !transform.Anchored)
            return false;

        return _tag.HasAnyTag(target, StructureTag, WallTag);
    }

    private void OnProjectileHit(Entity<RMCBruteProjectileComponent> projectile, ref AfterProjectileHitEvent args)
    {
        Prime(projectile);
    }

    private void OnProjectileFixedDistanceStop(Entity<RMCBruteProjectileComponent> projectile, ref ProjectileFixedDistanceStopEvent args)
    {
        Prime(projectile);
    }

    private void Prime(Entity<RMCBruteProjectileComponent> projectile)
    {
        if (projectile.Comp.Primed)
            return;

        projectile.Comp.Primed = true;

        var origin = _transform.GetMapCoordinates(projectile.Owner);
        var centered = new MapCoordinates(
            new Vector2(MathF.Floor(origin.Position.X) + 0.5f, MathF.Floor(origin.Position.Y) + 0.5f),
            origin.MapId);
        var forward = ToTileVector(projectile.Comp.LastDirection.GetDir());
        var right = new Vector2i(forward.Y, -forward.X);
        var seen = new HashSet<Vector2i>();

        for (var i = 0; i <= projectile.Comp.MaxDistance; i++)
        {
            var row = i;
            Timer.Spawn(TimeSpan.FromSeconds(projectile.Comp.RowDelay * row), () => DetonateRow(projectile.Comp, centered, forward, right, row, seen));
        }

        QueueDel(projectile.Owner);
    }

    private void DetonateRow(
        RMCBruteProjectileComponent component,
        MapCoordinates origin,
        Vector2i forward,
        Vector2i right,
        int row,
        HashSet<Vector2i> seen)
    {
        var center = forward * row;
        DetonateTile(component, origin, center, false, seen);

        var maxWidth = row == 1 || row == component.MaxDistance ? 1 : 2;
        for (var width = 1; width <= maxWidth; width++)
        {
            var edge = width == maxWidth;
            DetonateTile(component, origin, center + right * width, edge, seen);
            DetonateTile(component, origin, center - right * width, edge, seen);

            if (row <= 2)
                continue;

            DetonateTile(component, origin, center + right * width + forward, edge, seen);
            DetonateTile(component, origin, center - right * width + forward, edge, seen);
        }
    }

    private void DetonateTile(
        RMCBruteProjectileComponent component,
        MapCoordinates origin,
        Vector2i offset,
        bool edge,
        HashSet<Vector2i> seen)
    {
        if (!seen.Add(offset))
            return;

        var coordinates = new MapCoordinates(origin.Position + new Vector2(offset.X, offset.Y), origin.MapId);
        var damageAmount = edge
            ? _random.Next(component.EdgeLowerDamage, component.EdgeUpperDamage + 1)
            : component.StructureDamage;
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                ["Structural"] = damageAmount,
            },
        };

        foreach (var damageable in _lookup.GetEntitiesInRange<DamageableComponent>(coordinates, 0.45f))
        {
            if (!IsWaveStructure(damageable.Owner))
                continue;

            _damageable.TryChangeDamage(damageable.Owner, damage, true, damageable: damageable.Comp);
        }

        if (_random.Prob(component.FireChance))
            Spawn(component.FirePrototype, coordinates);

        if (_random.Prob(component.SparkChance))
            Spawn(component.SparkPrototype, coordinates);

        if (_random.Prob(component.SmokeChance))
            Spawn(component.SmokePrototype, coordinates);

        ThrowEntities(component, origin, coordinates, offset);
    }

    private bool IsWaveStructure(EntityUid target)
    {
        return !TerminatingOrDeleted(target) &&
               _tag.HasAnyTag(target, StructureTag, WallTag);
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
                var size = _size.TryGetSize(ent.Owner, out var foundSize)
                    ? (int) foundSize
                    : 1;

                skipChance += size * component.LivingSizeSkipChance;
            }

            if (_random.Prob(Math.Clamp(skipChance, 0, 1)))
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
        Spawn(launcher.Comp.SmokePrototype, back);
        _audio.PlayPvs(launcher.Comp.Sound, back);

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

    private static Vector2i ToTileVector(Direction direction)
    {
        var vector = direction.ToVec();
        var x = Math.Clamp((int) MathF.Round(vector.X), -1, 1);
        var y = Math.Clamp((int) MathF.Round(vector.Y), -1, 1);

        if (x == 0 && y == 0)
            x = 1;

        return new Vector2i(x, y);
    }
}
