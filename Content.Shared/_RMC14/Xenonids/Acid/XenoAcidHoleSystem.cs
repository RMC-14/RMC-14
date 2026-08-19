using System.Numerics;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.Barricade.Components;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.ClawSharpness;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Acid;

public sealed class XenoAcidHoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCPullingSystem _pulling = default!;
    [Dependency] private readonly RMCRepairableSystem _repairable = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoClawsSystem _xenoClaws = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<OccluderComponent> _occluderQuery;
    private EntityQuery<ReceiverXenoClawsComponent> _receiverClawsQuery;
    private EntityQuery<XenoClawsComponent> _clawsQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoAcidHoleWallComponent> _holeWallQuery;

    private static readonly ProtoId<StackPrototype> PlasteelStack = "CMPlasteel";
    private static readonly ProtoId<DamageGroupPrototype> AcidHoleDamage = "Brute";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly EntProtoId DamagedGirderPrototype = "RMCGirderDamaged";
    private const CollisionGroup HoleBlockMask = CollisionGroup.Impassable |
                                                 CollisionGroup.HighImpassable |
                                                 CollisionGroup.LowImpassable |
                                                 CollisionGroup.InteractImpassable |
                                                 CollisionGroup.BarbedBarricade;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _occluderQuery = GetEntityQuery<OccluderComponent>();
        _receiverClawsQuery = GetEntityQuery<ReceiverXenoClawsComponent>();
        _clawsQuery = GetEntityQuery<XenoClawsComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _holeWallQuery = GetEntityQuery<XenoAcidHoleWallComponent>();

        SubscribeLocalEvent<XenoAcidHoleComponent, ActivateInWorldEvent>(OnHoleActivateInWorld);
        SubscribeLocalEvent<XenoAcidHoleComponent, XenoAcidHoleCrawlDoAfterEvent>(OnHoleCrawlDoAfter);
        SubscribeLocalEvent<XenoAcidHoleComponent, InteractUsingEvent>(OnHoleInteractUsing, before: [typeof(XenoNestSystem)]);
        SubscribeLocalEvent<XenoAcidHoleComponent, XenoAcidHoleRepairDoAfterEvent>(OnHoleRepairDoAfter);
        SubscribeLocalEvent<XenoAcidHoleComponent, XenoAcidHoleBreakDoAfterEvent>(OnHoleBreakDoAfter);
        SubscribeLocalEvent<XenoAcidHoleComponent, GettingAttackedAttemptEvent>(OnHoleAttacked);
        SubscribeLocalEvent<XenoAcidHoleComponent, EntityTerminatingEvent>(OnHoleTerminating);

        SubscribeLocalEvent<XenoAcidHoleWallComponent, DamageChangedEvent>(OnWallDamageChanged);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, DamageModifyEvent>(OnWallDamageModify);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, GettingAttackedAttemptEvent>(OnWallAttacked);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, ActivateInWorldEvent>(OnWallActivateInWorld);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, InteractUsingEvent>(OnWallInteractUsing, before: [typeof(XenoNestSystem)]);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, RMCRepairableTargetAttemptEvent>(OnWallRepairAttempt);
        SubscribeLocalEvent<XenoAcidHoleWallComponent, EntityTerminatingEvent>(OnWallTerminating);

        SubscribeLocalEvent<XenoWallWeedsComponent, InteractHandEvent>(OnWeededWallInteractHand, before: [typeof(XenoNestSystem)]);
        SubscribeLocalEvent<XenoWallWeedsComponent, InteractUsingEvent>(OnWeededWallInteractUsing, before: [typeof(XenoNestSystem)]);
    }

    public bool HasActiveHole(EntityUid wall)
    {
        return _holeWallQuery.TryComp(wall, out var wallComp) &&
               HasActiveHole((wall, wallComp));
    }

    public void TryStoreAcidDirection(EntityUid wall, EntityUid attacker)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<XenoAcidHoleWallComponent>(wall, out var wallComp))
            return;

        if (!TryGetHoleDirection(wall, attacker, out var direction))
            return;

        wallComp.PendingDirection = direction;
    }

    public bool TryCreateHoleFromMelt(EntityUid wall)
    {
        if (_net.IsClient)
            return false;

        if (!TryComp<XenoAcidHoleWallComponent>(wall, out var wallComp))
            return false;

        if (TerminatingOrDeleted(wall))
            return false;

        if (HasActiveHole((wall, wallComp)))
        {
            wallComp.PendingDirection = null;
            return true;
        }

        if (_damageableQuery.TryComp(wall, out var damageable) &&
            _receiverClawsQuery.TryComp(wall, out var receiver))
        {
            var targetDamage = FixedPoint2.New(receiver.MaxHealth * 0.9f);
            if (damageable.TotalDamage < targetDamage)
                SetWallDamage((wall, damageable), targetDamage);
        }

        var direction = wallComp.PendingDirection ?? Direction.South;
        if (!TryCreateHole((wall, wallComp), direction))
            return false;

        wallComp.PendingDirection = null;
        _audio.PlayPvs(wallComp.HoleCreatedSound, wall);
        return true;
    }

    private void OnWallDamageChanged(Entity<XenoAcidHoleWallComponent> wall, ref DamageChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!args.DamageIncreased)
            return;

        if (!_damageableQuery.TryComp(wall, out var damageable))
            return;

        if (!_receiverClawsQuery.TryComp(wall, out var receiver))
            return;

        if (!IsDamageNearCap((wall, wall.Comp), damageable, receiver))
            return;

        if (HasActiveHole((wall, wall.Comp)))
            return;

        var attacker = args.Tool ?? args.Origin;
        if (attacker == null)
            return;

        if (!_xenoQuery.TryComp(attacker.Value, out var xeno) ||
            !HasRequiredClaws(receiver, attacker.Value, xeno))
        {
            return;
        }

        if (!TryGetHoleDirection(wall.Owner, attacker.Value, out var direction))
            return;

        TryCreateHole(wall, direction);
    }

    private void OnWallDamageModify(Entity<XenoAcidHoleWallComponent> wall, ref DamageModifyEvent args)
    {
        if (!HasActiveHole(wall))
            return;

        if (args.Tool is not { } tool)
            return;

        if (!_clawsQuery.HasComp(tool))
            return;

        args.Damage = new DamageSpecifier();
    }

    private void OnWallAttacked(Entity<XenoAcidHoleWallComponent> wall, ref GettingAttackedAttemptEvent args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        if (!TryGetActiveHole(wall, out var hole))
            return;

        TryStartBreak(args.Attacker, hole);
    }

    private void OnWallTerminating(Entity<XenoAcidHoleWallComponent> wall, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (wall.Comp.Hole is not { Valid: true } hole ||
            TerminatingOrDeleted(hole))
        {
            return;
        }

        QueueDel(hole);
    }

    private void OnHoleTerminating(Entity<XenoAcidHoleComponent> hole, ref EntityTerminatingEvent args)
    {
        if (hole.Comp.Wall is not { Valid: true } wall ||
            TerminatingOrDeleted(wall) ||
            !TryComp<XenoAcidHoleWallComponent>(wall, out var wallComp))
        {
            return;
        }

        if (wallComp.Hole != hole.Owner)
            return;

        ClearHole((wall, wallComp), deleteHole: false);
    }

    private void OnHoleActivateInWorld(Entity<XenoAcidHoleComponent> hole, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (TryStartBreak(args.User, hole))
            return;

        TryStartCrawl(args.User, hole);
    }

    private void OnHoleCrawlDoAfter(Entity<XenoAcidHoleComponent> hole, ref XenoAcidHoleCrawlDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryGetCrawlExit(args.User, hole, out var exit))
            return;

        args.Handled = true;

        _pulling.TryStopAllPullsFromAndOn(args.User);
        _transform.SetCoordinates(args.User, exit);
        _transform.AttachToGridOrMap(args.User);
    }

    private void OnHoleInteractUsing(Entity<XenoAcidHoleComponent> hole, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (HasComp<WelderComponent>(args.Used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-repair-requires-nailgun"), hole.Owner, args.User, PopupType.MediumCaution);
            return;
        }

        if (HasComp<NailgunComponent>(args.Used))
        {
            TryStartRepair(args.User, args.Used, hole);
            return;
        }

        if (TryStartBreak(args.User, hole))
            return;

        TryStartCrawl(args.User, hole);
    }

    private void OnHoleRepairDoAfter(Entity<XenoAcidHoleComponent> hole, ref XenoAcidHoleRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Used is not { } used)
            return;

        if (!TryComp<NailgunComponent>(used, out var nailgun))
            return;

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        if (!_repairable.TryGetNailgunRepairStack((args.User, hands), hole.Comp.RepairMaterialCost, out var stackUid, out var stack, PlasteelStack))
        {
            _popup.PopupEntity(Loc.GetString("rmc-nailgun-lost-stack"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(used, ref ammoCountEv);
        if (ammoCountEv.Count < hole.Comp.RepairNailCost)
        {
            _popup.PopupEntity(Loc.GetString("rmc-nailgun-no-nails-message"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryGetHoleWall(hole, out var wall))
            return;

        var repairValue = _repairable.GetNailgunRepairValue(wall.Owner, PlasteelStack);
        if (repairValue <= 0)
            return;

        args.Handled = true;

        var selfMsg = Loc.GetString("rmc-nailgun-finish-self", ("material", stackUid), ("target", hole.Owner));
        var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", args.User), ("material", stackUid), ("target", hole.Owner));
        _popup.PopupPredicted(selfMsg, othersMsg, args.User, args.User);
        _audio.PlayPredicted(nailgun.RepairSound, wall.Owner, args.User);

        if (_net.IsClient)
            return;

        if (!_stack.Use(stackUid, hole.Comp.RepairMaterialCost, stack))
            return;

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        var takeAmmo = new TakeAmmoEvent(hole.Comp.RepairNailCost, ammo, Transform(args.User).Coordinates, args.User);
        RaiseLocalEvent(used, takeAmmo);

        foreach (var (bullet, _) in takeAmmo.Ammo)
        {
            if (bullet != null)
                QueueDel(bullet.Value);
        }

        if (_damageableQuery.TryComp(wall, out var damageable))
        {
            var heal = -_rmcDamageable.DistributeTypesTotal(wall.Owner, FixedPoint2.New(repairValue));
            _damageable.TryChangeDamage(wall, heal, true, damageable: damageable);
        }

        ClearHole(wall, deleteHole: true);
    }

    private void OnHoleBreakDoAfter(Entity<XenoAcidHoleComponent> hole, ref XenoAcidHoleBreakDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled)
            return;

        if (!TryGetHoleWall(hole, out var wall))
            return;

        if (!CanBreakHole(args.User, wall))
            return;

        args.Handled = true;

        ReplaceWallWithDamagedGirder(wall);
    }

    private void OnHoleAttacked(Entity<XenoAcidHoleComponent> hole, ref GettingAttackedAttemptEvent args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        TryStartBreak(args.Attacker, hole);
    }

    private bool TryGetHoleWall(Entity<XenoAcidHoleComponent> hole, out Entity<XenoAcidHoleWallComponent> wall)
    {
        wall = default;
        if (hole.Comp.Wall is not { Valid: true } wallUid ||
            TerminatingOrDeleted(wallUid) ||
            !TryComp<XenoAcidHoleWallComponent>(wallUid, out var wallComp))
        {
            return false;
        }

        wall = (wallUid, wallComp);
        return true;
    }

    private bool TryGetCrawlExit(EntityUid user, Entity<XenoAcidHoleComponent> hole, out EntityCoordinates exit)
    {
        exit = default;

        if (!TryGetHoleWall(hole, out var wall))
            return false;

        if (!TryGetCrawlExitDirection(user, wall.Owner, hole.Comp.EntranceDirection, out var exitDirection))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-wrong-side"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        if (!CanCrossHoleSide(user, hole, wall.Owner))
            return false;

        var wallCoords = _transform.GetMoverCoordinates(wall.Owner);
        exit = wallCoords.Offset(exitDirection.ToVec());

        if (!_turf.TryGetTileRef(exit, out var tile))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-no-exit"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        if (_turf.IsTileBlocked(tile.Value, HoleBlockMask) || IsHoleSideBlocked(wallCoords.Position, exit.Position, user, hole, wall.Owner))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-blocked"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private bool CanReachHole(EntityUid user, Entity<XenoAcidHoleComponent> hole)
    {
        if (!TryGetHoleWall(hole, out var wall))
            return false;

        if (!TryGetHoleSide(user, wall.Owner, hole.Comp.EntranceDirection, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-wrong-side"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        return CanCrossHoleSide(user, hole, wall.Owner);
    }

    private bool CanCrossHoleSide(EntityUid user, Entity<XenoAcidHoleComponent> hole, EntityUid wall)
    {
        var userCoords = _transform.GetMoverCoordinates(user);
        var wallCoords = _transform.GetMoverCoordinates(wall);

        if (!userCoords.TryDistance(EntityManager, _transform, wallCoords, out var distance) ||
            distance > SharedInteractionSystem.InteractionRange)
        {
            _popup.PopupClient(Loc.GetString("interaction-system-user-interaction-cannot-reach"), user, PopupType.SmallCaution);
            return false;
        }

        if (IsHoleSideBlocked(userCoords.Position, wallCoords.Position, user, hole, wall))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-entrance-blocked"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private bool TryGetCrawlExitDirection(EntityUid user, EntityUid wall, Direction entrance, out Direction exitDirection)
    {
        exitDirection = default;
        if (!TryGetHoleSide(user, wall, entrance, out var approach))
            return false;

        var opposite = entrance.GetOpposite();

        if (approach == entrance)
        {
            exitDirection = opposite;
            return true;
        }

        if (approach == opposite)
        {
            exitDirection = entrance;
            return true;
        }

        return false;
    }

    private bool TryGetHoleSide(EntityUid user, EntityUid wall, Direction entrance, out Direction side)
    {
        side = default;
        var userCoords = _transform.GetMoverCoordinates(user).SnapToGrid(EntityManager);
        var wallCoords = _transform.GetMoverCoordinates(wall).SnapToGrid(EntityManager);

        if (!userCoords.TryDelta(EntityManager, _transform, wallCoords, out var delta) ||
            delta == Vector2.Zero)
        {
            return false;
        }

        var approach = delta.GetDir();
        var opposite = entrance.GetOpposite();
        if (approach != entrance && approach != opposite)
            return false;

        side = approach;
        return true;
    }

    private bool IsHoleSideBlocked(Vector2 start, Vector2 end, EntityUid user, Entity<XenoAcidHoleComponent> hole, EntityUid wall)
    {
        var direction = end - start;
        if (direction == Vector2.Zero)
            return false;

        var mapId = Transform(wall).MapID;
        var ray = new CollisionRay(start, direction.Normalized(), (int) HoleBlockMask);
        var hits = _physics.IntersectRayWithPredicate(
            mapId,
            ray,
            direction.Length(),
            uid =>
            {
                if (uid == user || uid == hole.Owner || uid == wall)
                    return true;

                return TryComp(uid, out TransformComponent? xform) && !xform.Anchored;
            },
            false);

        foreach (var hit in hits)
        {
            var blocker = hit.HitEntity;
            if (ShouldIgnoreHoleBlocker(blocker))
                continue;

            return true;
        }

        return false;
    }

    private bool ShouldIgnoreHoleBlocker(EntityUid blocker)
    {
        if (HasComp<BarricadeComponent>(blocker))
            return !TryComp<BarbedComponent>(blocker, out var barbed) || !barbed.IsBarbed;

        if (HasComp<DirectionalAttackBlockerComponent>(blocker))
            return false;

        return HasComp<ClimbableComponent>(blocker);
    }

    private bool TryGetHoleDirection(EntityUid wall, EntityUid attacker, out Direction direction)
    {
        direction = Direction.Invalid;
        var wallCoords = _transform.GetMoverCoordinates(wall);
        var attackerCoords = _transform.GetMoverCoordinates(attacker);

        if (!attackerCoords.TryDelta(EntityManager, _transform, wallCoords, out var delta) ||
            delta == Vector2.Zero)
        {
            return false;
        }

        var attemptedDir = delta.GetDir();
        if (attemptedDir.IsCardinal())
        {
            direction = attemptedDir;
            return true;
        }

        var flags = attemptedDir.AsFlag();
        direction = ResolveDiagonal(wallCoords, delta, flags);
        return true;
    }

    private Direction ResolveDiagonal(EntityCoordinates wallCoords, Vector2 delta, DirectionFlag flags)
    {
        Direction? openDir = null;
        foreach (var dir in _rmcMap.CardinalDirections)
        {
            if (!flags.HasFlag(dir.AsFlag()))
                continue;

            if (HasWallNeighbor(wallCoords, dir))
                continue;

            if (openDir == null)
            {
                openDir = dir;
                continue;
            }

            openDir = null;
            break;
        }

        if (openDir != null)
            return openDir.Value;

        if (MathF.Abs(delta.X) >= MathF.Abs(delta.Y))
            return delta.X >= 0 ? Direction.East : Direction.West;

        return delta.Y >= 0 ? Direction.North : Direction.South;
    }

    private bool HasWallNeighbor(EntityCoordinates wallCoords, Direction direction)
    {
        return _rmcMap.TileHasAnyTag(wallCoords.Offset(direction.ToVec()), WallTag);
    }

    private bool IsDamageNearCap(Entity<XenoAcidHoleWallComponent> wall, DamageableComponent damageable, ReceiverXenoClawsComponent receiver)
    {
        var threshold = FixedPoint2.New(receiver.MaxHealth * wall.Comp.DamageNearCapRatio);
        return damageable.TotalDamage >= threshold;
    }

    private bool HasRequiredClaws(ReceiverXenoClawsComponent receiver, EntityUid attacker, XenoComponent xeno)
    {
        if (receiver.MinimumXenoTier != null && xeno.Tier >= receiver.MinimumXenoTier)
            return true;

        return _xenoClaws.HasClawStrength(attacker, receiver.MinimumClawStrength);
    }

    private bool TryCreateHole(Entity<XenoAcidHoleWallComponent> wall, Direction direction)
    {
        if (HasActiveHole(wall))
            return false;

        var holeRotation = direction.ToAngle() - Transform(wall).WorldRotation;
        var hole = SpawnAttachedTo(wall.Comp.HolePrototype, wall.Owner.ToCoordinates(), rotation: holeRotation);
        var holeComp = EnsureComp<XenoAcidHoleComponent>(hole);
        holeComp.Wall = wall.Owner;
        holeComp.EntranceDirection = direction;
        Dirty(hole, holeComp);

        wall.Comp.Hole = hole;
        Dirty(wall);

        if (_occluderQuery.TryComp(wall, out var occluder))
            _occluder.SetEnabled(wall.Owner, false, occluder);

        return true;
    }

    private void SetWallDamage(Entity<DamageableComponent> wall, FixedPoint2 damage)
    {
        var damageSpecifier = new DamageSpecifier(_prototype.Index(AcidHoleDamage), damage);
        _damageable.SetDamage(wall.Owner, wall.Comp, damageSpecifier);
    }

    private void ReplaceWallWithDamagedGirder(Entity<XenoAcidHoleWallComponent> wall)
    {
        ClearHole(wall, deleteHole: true);

        var xform = Transform(wall);
        var damagedGirder = Spawn(DamagedGirderPrototype, xform.Coordinates);
        _transform.SetLocalRotation(damagedGirder, xform.LocalRotation);
        QueueDel(wall);
    }

    private bool HasActiveHole(Entity<XenoAcidHoleWallComponent> wall)
    {
        if (wall.Comp.Hole is { Valid: true } hole && !TerminatingOrDeleted(hole))
            return true;

        wall.Comp.Hole = null;
        Dirty(wall);
        return false;
    }

    private void ClearHole(Entity<XenoAcidHoleWallComponent> wall, bool deleteHole)
    {
        var hole = wall.Comp.Hole;
        wall.Comp.Hole = null;
        wall.Comp.PendingDirection = null;
        Dirty(wall);

        if (!TerminatingOrDeleted(wall.Owner) &&
            _occluderQuery.TryComp(wall, out var occluder))
        {
            _occluder.SetEnabled(wall.Owner, true, occluder);
        }

        if (deleteHole &&
            hole is { Valid: true } holeUid &&
            !TerminatingOrDeleted(holeUid))
        {
            QueueDel(holeUid);
        }
    }

    private void OnWallActivateInWorld(Entity<XenoAcidHoleWallComponent> wall, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActiveHole(wall, out var hole))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (TryStartBreak(args.User, hole))
            return;

        TryStartCrawl(args.User, hole);
    }

    private void OnWallInteractUsing(Entity<XenoAcidHoleWallComponent> wall, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActiveHole(wall, out var hole))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (HasComp<WelderComponent>(args.Used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-repair-requires-nailgun"), wall.Owner, args.User, PopupType.MediumCaution);
            return;
        }

        if (HasComp<NailgunComponent>(args.Used))
        {
            TryStartRepair(args.User, args.Used, hole);
            return;
        }

        if (TryStartBreak(args.User, hole))
            return;

        TryStartCrawl(args.User, hole);
    }

    private void OnWeededWallInteractHand(Entity<XenoWallWeedsComponent> weeds, ref InteractHandEvent args)
    {
        if (args.Handled ||
            !TryGetWeededWallWithActiveHole(weeds.Owner, out var wall))
        {
            return;
        }

        var ev = new InteractHandEvent(args.User, wall.Owner);
        RaiseLocalEvent(wall.Owner, ev);
        args.Handled = ev.Handled;
    }

    private void OnWeededWallInteractUsing(Entity<XenoWallWeedsComponent> weeds, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryGetWeededWallWithActiveHole(weeds.Owner, out var wall))
        {
            return;
        }

        var ev = new InteractUsingEvent(args.User, args.Used, wall.Owner, args.ClickLocation);
        RaiseLocalEvent(wall.Owner, ev);
        args.Handled = ev.Handled;
    }

    private void OnWallRepairAttempt(Entity<XenoAcidHoleWallComponent> wall, ref RMCRepairableTargetAttemptEvent args)
    {
        if (!HasActiveHole(wall.Owner))
            return;

        args.Cancelled = true;
        args.Popup = _xenoQuery.HasComp(args.User)
            ? Loc.GetString("rmc-acid-hole-repair-blocked")
            : Loc.GetString("rmc-acid-hole-repair-requires-nailgun");
    }

    private bool TryGetWeededWallWithActiveHole(EntityUid weeds, out Entity<XenoAcidHoleWallComponent> wall)
    {
        wall = default;
        if (!TryComp(weeds, out XenoNestSurfaceComponent? surface) ||
            surface.Weedable is not { Valid: true } coveredWall ||
            !TryComp<XenoAcidHoleWallComponent>(coveredWall, out var wallComp) ||
            !HasActiveHole((coveredWall, wallComp)))
        {
            return false;
        }

        wall = (coveredWall, wallComp);
        return true;
    }

    private bool TryGetActiveHole(Entity<XenoAcidHoleWallComponent> wall, out Entity<XenoAcidHoleComponent> hole)
    {
        hole = default;
        if (wall.Comp.Hole is not { Valid: true } holeUid ||
            TerminatingOrDeleted(holeUid) ||
            !TryComp<XenoAcidHoleComponent>(holeUid, out var holeComp))
        {
            return false;
        }

        hole = (holeUid, holeComp);
        return true;
    }

    private void TryStartCrawl(EntityUid user, Entity<XenoAcidHoleComponent> hole)
    {
        if (!CanCrawl(user, hole))
            return;

        var ev = new XenoAcidHoleCrawlDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, hole.Comp.CrawlDelay, ev, hole, hole)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private bool CanCrawl(EntityUid user, Entity<XenoAcidHoleComponent> hole)
    {
        if (!_xenoQuery.TryComp(user, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-too-large-non-xeno"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        if (!_size.TryGetSize(user, out var size) || size > RMCSizes.Xeno)
        {
            _popup.PopupEntity(Loc.GetString("rmc-acid-hole-only-small-xenos"), hole.Owner, user, PopupType.SmallCaution);
            return false;
        }

        if (!CanReachHole(user, hole))
            return false;

        if (!TryGetCrawlExit(user, hole, out _))
            return false;

        return true;
    }

    private bool TryStartBreak(EntityUid user, Entity<XenoAcidHoleComponent> hole)
    {
        if (!TryGetHoleWall(hole, out var wall))
            return false;

        if (!CanBreakHole(user, wall))
            return false;

        var ev = new XenoAcidHoleBreakDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, hole.Comp.BreakDelay, ev, hole, hole)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = false,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _audio.PlayPvs(wall.Comp.HoleExpandSound, wall.Owner);
        return true;
    }

    private bool CanBreakHole(EntityUid user, Entity<XenoAcidHoleWallComponent> wall)
    {
        if (!HasActiveHole(wall.Owner))
            return false;

        if (!_xenoQuery.TryComp(user, out _))
            return false;

        if (!_size.TryGetSize(user, out var size) || size < RMCSizes.Big)
            return false;

        return true;
    }

    private void TryStartRepair(EntityUid user, EntityUid used, Entity<XenoAcidHoleComponent> hole)
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return;

        if (!_repairable.TryGetNailgunRepairStack((user, hands), hole.Comp.RepairMaterialCost, out _, out _, PlasteelStack))
        {
            _popup.PopupEntity(Loc.GetString("rmc-nailgun-no-material-message", ("target", hole.Owner)), user, user, PopupType.SmallCaution);
            return;
        }

        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(used, ref ammoCountEv);
        if (ammoCountEv.Count < hole.Comp.RepairNailCost)
        {
            _popup.PopupEntity(Loc.GetString("rmc-nailgun-no-nails-message"), user, user, PopupType.SmallCaution);
            return;
        }

        var ev = new XenoAcidHoleRepairDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, hole.Comp.RepairDelay, ev, hole, used: used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-repairable-start-self", ("target", hole.Owner));
            var othersMsg = Loc.GetString("rmc-repairable-start-others", ("user", user), ("target", hole.Owner));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }
}
