using System.Linq;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Directions;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Atmos;

public abstract class SharedRMCFlammableSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;

    private static readonly ProtoId<AlertPrototype> FireAlert = "Fire";
    private static readonly ProtoId<ReagentPrototype> WaterReagent = "Water";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    private EntityQuery<BlockTileFireComponent> _blockTileFireQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<FlammableComponent> _flammableQuery;
    private EntityQuery<RMCIgniteOnCollideComponent> _igniteOnCollideQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<TileFireComponent> _tileFireQuery;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _fireContacts = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _extinguishContacts = new();
    private readonly HashSet<EntityUid> _entitiesNeedingRecheck = new();
    private float _recheckAccumulator = 0f;
    private float _steppingAccumulator = 0f;
    private float _cleanupAccumulator = 0f;
    private const float RecheckInterval = 0.5f;
    private const float SteppingUpdateInterval = 0.1f;
    private const float CleanupInterval = 5.0f;

    public override void Initialize()
    {
        _blockTileFireQuery = GetEntityQuery<BlockTileFireComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _flammableQuery = GetEntityQuery<FlammableComponent>();
        _igniteOnCollideQuery = GetEntityQuery<RMCIgniteOnCollideComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _tileFireQuery = GetEntityQuery<TileFireComponent>();

        SubscribeLocalEvent<IgniteOnProjectileHitComponent, ProjectileHitEvent>(OnIgniteOnProjectileHit);

        SubscribeLocalEvent<TileFireComponent, MapInitEvent>(OnTileFireMapInit);
        SubscribeLocalEvent<TileFireComponent, VaporHitEvent>(OnTileFireVaporHit);
        SubscribeLocalEvent<TileFireComponent, InteractHandEvent>(OnTileFireInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<TileFireComponent, PreventCollideEvent>(OnTileFirePreventCollide);

        SubscribeLocalEvent<CraftsIntoMolotovComponent, ExaminedEvent>(OnCraftsIntoMolotovExamined);
        SubscribeLocalEvent<CraftsIntoMolotovComponent, InteractUsingEvent>(OnCraftsIntoMolotovInteractUsing);
        SubscribeLocalEvent<CraftsIntoMolotovComponent, CraftMolotovDoAfterEvent>(OnCraftsIntoMolotovDoAfter);

        SubscribeLocalEvent<TileFireOnTriggerComponent, RMCTriggerEvent>(OnTileFireTriggered);
        SubscribeLocalEvent<TileFireOnTriggerComponent, CMExplosiveTriggeredEvent>(OnTileFireOnTriggerExplosive);

        SubscribeLocalEvent<DirectionalTileFireOnTriggerComponent, RMCTriggerEvent>(OnDirectionTileFireTriggered);

        SubscribeLocalEvent<SteppingOnFireComponent, CMGetArmorEvent>(OnSteppingOnFireGetArmor);
        SubscribeLocalEvent<SteppingOnFireComponent, ComponentRemove>(OnSteppingOnFireRemoved);

        SubscribeLocalEvent<CanBeFirePattedComponent, InteractHandEvent>(OnCanBeFirePattedInteractHand, before: [typeof(InteractionPopupSystem)]);

        SubscribeLocalEvent<FlammableComponent, RMCIgniteEvent>(OnFlammableIgnite);
        SubscribeLocalEvent<FlammableComponent, RMCExtinguishedEvent>(OnFlammableExtinguished);

        SubscribeLocalEvent<PlasmaFrenzyComponent, RMCIgniteEvent>(OnPlasmaFrenzyIgnite);

        SubscribeLocalEvent<RMCIgniteOnCollideComponent, StartCollideEvent>(OnIgniteStartCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, EndCollideEvent>(OnIgniteEndCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, DamageCollideEvent>(OnIgniteDamageCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, ComponentInit>(OnIgniteComponentInit);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, ComponentRemove>(OnIgniteRemoved);
        SubscribeLocalEvent<ExtinguishFireComponent, StartCollideEvent>(OnExtinguishStartCollide);
        SubscribeLocalEvent<ExtinguishFireComponent, EndCollideEvent>(OnExtinguishEndCollide);
        SubscribeLocalEvent<ExtinguishFireComponent, ComponentRemove>(OnExtinguishRemoved);
    }

    private void OnIgniteOnProjectileHit(Entity<IgniteOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.Duration, false);
    }

    private void OnTileFireMapInit(Entity<TileFireComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpawnedAt = _timing.CurTime;
        Dirty(ent);
    }

    private void OnTileFireVaporHit(Entity<TileFireComponent> ent, ref VaporHitEvent args)
    {
        if (_net.IsClient) return;
        var water = false;

        foreach (var container in args.Solution.Comp.Containers)
        {
            if (!_solutionContainer.TryGetSolution(args.Solution.Owner, container, out _, out var solution))
                continue;

            if (solution.ContainsPrototype(WaterReagent))
            {
                water = true;
                break;
            }
        }

        if (!water) return;

        if (ent.Comp.ExtinguishInstantly)
        {
            QueueDel(ent);
            return;
        }

        ent.Comp.Duration -= TimeSpan.FromSeconds(7);
        Dirty(ent);
    }

    private void OnTileFireInteractHand(Entity<TileFireComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out TileFirePatterComponent? patter))
            return;

        var time = _timing.CurTime;
        if (time < patter.Last + patter.Cooldown) return;

        patter.Last = time;
        Dirty(user, patter);

        ent.Comp.Duration -= patter.RemoveDuration * ent.Comp.PatExtinguishMultiplier;
        Dirty(ent);

        _rmcMelee.DoLunge(user, ent);
        _audio.PlayPredicted(patter.Sound, user, user, AudioParams.Default.WithVolume(-8).WithVariation(0.05f));
    }

    private void OnTileFirePreventCollide(Entity<TileFireComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled) return;
        if (_projectileQuery.HasComp(args.OtherEntity) ||
            _tileFireQuery.HasComp(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    private void OnCraftsIntoMolotovExamined(Entity<CraftsIntoMolotovComponent> ent, ref ExaminedEvent args)
    {
        if (!CanCraftMolotovPopup(ent, args.Examiner, false, out _))
            return;
        using (args.PushGroup(nameof(CraftsIntoMolotovComponent)))
        {
            args.PushMarkup("[color=cyan]You can turn this into a molotov with a piece of paper![/color]");
        }
    }

    private void OnCraftsIntoMolotovInteractUsing(Entity<CraftsIntoMolotovComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<PaperComponent>(args.Used))
            return;

        if (!CanCraftMolotovPopup(ent, args.User, true, out _))
            return;

        var ev = new CraftMolotovDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, ent, args.Used)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnCraftsIntoMolotovDoAfter(Entity<CraftsIntoMolotovComponent> ent, ref CraftMolotovDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!HasComp<PaperComponent>(args.Used))
            return;

        if (!CanCraftMolotovPopup(ent, args.User, true, out var intensity))
            return;

        if (_net.IsClient) return;

        var coords = _transform.GetMoverCoordinates(ent);
        var molotov = Spawn(ent.Comp.Spawns, coords);
        var tileFire = EnsureComp<TileFireOnTriggerComponent>(molotov);

        tileFire.Duration = intensity.Int();
        Dirty(molotov, tileFire);

        Del(ent);
        Del(args.Used);

        _hands.TryPickupAnyHand(args.User, molotov);
    }

    private void OnTileFireTriggered(Entity<TileFireOnTriggerComponent> ent, ref RMCTriggerEvent args)
    {
        var coords = _transform.GetMoverCoordinates(ent);
        _audio.PlayPvs(ent.Comp.Sound, coords);
        var tile = coords.SnapToGrid(EntityManager, _map);

        SpawnFireDiamond(ent.Comp.Spawn, tile, ent.Comp.Range, ent.Comp.Intensity, ent.Comp.Duration);
        QueueDel(ent);
    }

    private void OnDirectionTileFireTriggered(Entity<DirectionalTileFireOnTriggerComponent> ent,
        ref RMCTriggerEvent args)
    {
        var moverCoordinates = _transform.GetMoverCoordinateRotation(ent, Transform(ent));
        var tile = moverCoordinates.Coords.SnapToGrid(EntityManager, _map);

        ent.Comp.Direction = Angle.FromDegrees(ent.Comp.Direction.ToAngle().Degrees + moverCoordinates.worldRot.Degrees).GetDir();
        Dirty(ent);

        if (ent.Comp.Rebounded)
            tile = tile.Offset(ent.Comp.Direction);

        _audio.PlayPvs(ent.Comp.Sound, moverCoordinates.Coords);

        SpawnFireCone(ent, tile, ent.Comp.Intensity, ent.Comp.Duration);
        QueueDel(ent);
    }

    private void OnTileFireOnTriggerExplosive(Entity<TileFireOnTriggerComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        var coords = _transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager, _map);
        SpawnFireDiamond(ent.Comp.Spawn, coords, ent.Comp.Range, ent.Comp.Intensity, ent.Comp.Duration);
    }

    private void OnSteppingOnFireRemoved(Entity<SteppingOnFireComponent> ent, ref ComponentRemove args)
    {
        _armor.UpdateArmorValue((ent, null));
    }

    private void OnSteppingOnFireGetArmor(Entity<SteppingOnFireComponent> ent, ref CMGetArmorEvent args)
    {
        args.ArmorModifier *= ent.Comp.ArmorMultiplier;
    }

    private void OnCanBeFirePattedInteractHand(Entity<CanBeFirePattedComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (args.Target != ent.Owner || user == args.Target || !TryComp(user, out FirePatterComponent? patter) ||
            _entityWhitelist.IsBlacklistPass(patter.Blacklist, ent) || !TryComp(ent, out FlammableComponent? flammable) ||
            !flammable.OnFire) return;

        args.Handled = true;
        var time = _timing.CurTime;
        if (time < patter.LastPat + patter.Cooldown)
            return;

        patter.LastPat = time;
        Dirty(user, patter);

        Pat(ent.Owner, patter.Stacks);

        _audio.PlayPredicted(patter.Sound, user, user);
        _popup.PopupClient($"You try to put out the fire on {Name(ent)}!", ent, user, PopupType.SmallCaution);
        _popup.PopupEntity($"{Name(user)} tries to put out the fire on you!", ent, ent, PopupType.SmallCaution);

        var others = Filter.PvsExcept(ent).RemoveWhereAttachedEntity(e => e == user || e == ent.Owner);
        _popup.PopupEntity($"{Name(user)} tries to put out the fire on {Name(ent)}!", ent, others, true);
    }

    private void OnFlammableIgnite(Entity<FlammableComponent> ent, ref RMCIgniteEvent args)
    {
        EnsureComp<OnFireComponent>(ent);
    }

    private void OnFlammableExtinguished(Entity<FlammableComponent> ent, ref RMCExtinguishedEvent args)
    {
        RemCompDeferred<OnFireComponent>(ent);
        if (IsEntityTouchingFire(ent.Owner))
        {
            _entitiesNeedingRecheck.Add(ent.Owner);
        }
    }

    private void OnIgniteStartCollide(Entity<RMCIgniteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!ShouldProcessIgniteCollision(ent.Comp, args.OurFixture, args.OtherFixture)) return;

        TrackFireContact(ent.Owner, args.OtherEntity);
        TryIgnite(ent, args.OtherEntity, false);

        if (_flammableQuery.HasComp(args.OtherEntity))
        {
            EnsureComp<SteppingOnFireComponent>(args.OtherEntity);
            _entitiesNeedingRecheck.Add(args.OtherEntity);
        }
    }

    private void OnIgniteEndCollide(Entity<RMCIgniteOnCollideComponent> ent, ref EndCollideEvent args)
    {
        UntrackFireContact(ent.Owner, args.OtherEntity);

        if (!IsEntityTouchingFire(args.OtherEntity))
        {
            RemCompDeferred<SteppingOnFireComponent>(args.OtherEntity);
            _entitiesNeedingRecheck.Remove(args.OtherEntity);
        }
    }

    private void OnIgniteDamageCollide(Entity<RMCIgniteOnCollideComponent> ent, ref DamageCollideEvent args)
    {
        Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks);
    }

    private void OnIgniteComponentInit(Entity<RMCIgniteOnCollideComponent> ent, ref ComponentInit args)
    {
        Timer.Spawn(TimeSpan.FromMilliseconds(50), () => CheckForExistingOverlaps(ent));
    }

    private void OnIgniteRemoved(Entity<RMCIgniteOnCollideComponent> ent, ref ComponentRemove args)
    {
        if (_fireContacts.TryGetValue(ent.Owner, out var contacts))
        {
            foreach (var contact in contacts)
            {
                if (!IsEntityTouchingFire(contact))
                {
                    RemCompDeferred<SteppingOnFireComponent>(contact);
                    _entitiesNeedingRecheck.Remove(contact);
                }
            }
        }
        _fireContacts.Remove(ent.Owner);
    }

    private void OnExtinguishStartCollide(Entity<ExtinguishFireComponent> ent, ref StartCollideEvent args)
    {
        if ((args.OurFixture.CollisionLayer & (int)ent.Comp.Collision) == 0) return;

        TrackExtinguishContact(ent.Owner, args.OtherEntity);
        TryExtinguishEntity(ent.Owner, args.OtherEntity);
    }

    private void OnExtinguishEndCollide(Entity<ExtinguishFireComponent> ent, ref EndCollideEvent args)
    {
        UntrackExtinguishContact(ent.Owner, args.OtherEntity);
    }

    private void OnExtinguishRemoved(Entity<ExtinguishFireComponent> ent, ref ComponentRemove args)
    {
        _extinguishContacts.Remove(ent.Owner);
    }

    private void CheckForExistingOverlaps(Entity<RMCIgniteOnCollideComponent> ent)
    {
        if (TerminatingOrDeleted(ent.Owner)) return;

        var intersecting = _physics.GetEntitiesIntersectingBody(ent.Owner, (int)ent.Comp.Collision);

        foreach (var entity in intersecting)
        {
            TrackFireContact(ent.Owner, entity);
            TryIgnite(ent, entity, false);
            if (_flammableQuery.HasComp(entity))
            {
                EnsureComp<SteppingOnFireComponent>(entity);
                _entitiesNeedingRecheck.Add(entity);
            }
        }
    }

    private void TrackFireContact(EntityUid fireSource, EntityUid target)
    {
        if (!_fireContacts.TryGetValue(fireSource, out var contacts))
        {
            contacts = new HashSet<EntityUid>();
            _fireContacts[fireSource] = contacts;
        }

        contacts.Add(target);
    }

    private void UntrackFireContact(EntityUid fireSource, EntityUid target)
    {
        if (_fireContacts.TryGetValue(fireSource, out var contacts))
        {
            contacts.Remove(target);
            if (contacts.Count == 0)
            {
                _fireContacts.Remove(fireSource);
            }
        }
    }

    private void TrackExtinguishContact(EntityUid extinguishSource, EntityUid target)
    {
        if (!_extinguishContacts.TryGetValue(extinguishSource, out var contacts))
        {
            contacts = new HashSet<EntityUid>();
            _extinguishContacts[extinguishSource] = contacts;
        }
        contacts.Add(target);
    }

    private void UntrackExtinguishContact(EntityUid extinguishSource, EntityUid target)
    {
        if (_extinguishContacts.TryGetValue(extinguishSource, out var contacts))
        {
            contacts.Remove(target);
            if (contacts.Count == 0)
                _extinguishContacts.Remove(extinguishSource);
        }
    }

    private bool IsEntityTouchingFire(EntityUid entity)
    {
        foreach (var contacts in _fireContacts.Values)
        {
            if (contacts.Contains(entity))
                return true;
        }
        return false;
    }

    private bool ShouldProcessIgniteCollision(RMCIgniteOnCollideComponent comp, Fixture ourFixture, Fixture otherFixture)
    {
        return (ourFixture.CollisionLayer & (int)comp.Collision) != 0;
    }

    private void TryExtinguishEntity(EntityUid extinguisher, EntityUid target)
    {
        if (!_flammableQuery.TryComp(target, out var flammable)) return;

        var ev = new ExtinguishFireAttemptEvent(extinguisher, target);
        RaiseLocalEvent(extinguisher, ref ev);
        if (!ev.Cancelled)
        {
            Extinguish((target, flammable));
        }
    }

    public void UpdateFireAlert(EntityUid ent)
    {
        var ev = new ShowFireAlertEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.Show)
            _alerts.ShowAlert(ent, FireAlert);
        else
            _alerts.ClearAlert(ent, FireAlert);
    }

    public bool IsOnFire(Entity<FlammableComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.OnFire;
    }

    public virtual bool Ignite(Entity<FlammableComponent?> flammable, int intensity, int duration, int? maxStacks, bool igniteDamage = true)
    {
        // TODO RMC14
        return false;
    }

    public virtual void Extinguish(Entity<FlammableComponent?> flammable) { }

    public virtual void Pat(Entity<FlammableComponent?> flammable, int stacks) { }

    private void SpawnFireChain(EntProtoId spawn, EntityUid chain, EntityCoordinates coordinates, int? intensity, int? duration)
    {
        var spawned = Spawn(spawn, coordinates);
        if (intensity != null || duration != null)
        {
            var ignite = EnsureComp<RMCIgniteOnCollideComponent>(spawned);
            if (intensity != null)
                ignite.Intensity = intensity.Value;

            if (duration != null)
                ignite.Duration = duration.Value;

            Dirty(spawned, ignite);
        }

        var onCollide = EnsureComp<DamageOnCollideComponent>(spawned);
        _onCollide.SetChain((spawned, onCollide), chain);
    }

    private void SpawnFires(EntProtoId spawn, EntityCoordinates coordinates, int range, EntityUid chain, int? intensity, int? duration, HashSet<EntityCoordinates>? spawned = null, List<Box2>? exclusionZones = null)
    {
        if (_net.IsClient) return;
        spawned ??= new HashSet<EntityCoordinates>();
        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var target = coordinates.Offset(cardinal);
            if (!spawned.Add(target))
                continue;

            var nextRange = SpawnFire(target, spawn, chain, range, intensity, duration, out var cont, exclusionZones);
            if (nextRange == 0 || cont)
                continue;

            Timer.Spawn(TimeSpan.FromMilliseconds(50), () =>
            {
                try { SpawnFires(spawn, target, nextRange, chain, intensity, duration, spawned, exclusionZones); }
                catch (Exception e) { Log.Error($"Error occurred spawning fires:\n{e}"); }
            });
        }
    }

    public void SpawnFireDiamond(EntProtoId spawn, EntityCoordinates center, int range, int? intensity = null, int? duration = null, List<Box2>? exclusionZones = null)
    {
        var chain = _onCollide.SpawnChain();
        SpawnFires(spawn, center, range, chain, intensity, duration, exclusionZones: exclusionZones);
    }

    public int SpawnFire(EntityCoordinates target, EntProtoId spawn, EntityUid chain, int range, int? intensity, int? duration, out bool cont, List<Box2>? exclusionZones = null)
    {
        cont = false;
        if (!_rmcMap.TryGetTileDef(target, out var tile) || tile.ID == ContentTileDefinition.SpaceID)
        {
            cont = true;
            return range;
        }

        if (exclusionZones != null && IsInExclusionZone(target, exclusionZones))
        {
            cont = true;
            return range;
        }

        if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(target, out var oldTileFire))
        {
            if (spawn == oldTileFire.Comp.Id)
            {
                cont = true;
                return range;
            }

            QueueDel(oldTileFire);
        }

        var nextRange = range - 1;
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(target);
        while (anchored.MoveNext(out var uid))
        {
            if (_blockTileFireQuery.HasComp(uid))
            {
                nextRange = 0;
                break;
            }

            if (_tag.HasAnyTag(uid, StructureTag, WallTag) &&
                !_doorQuery.HasComp(uid))
            {
                nextRange = 0;
                break;
            }
        }

        SpawnFireChain(spawn, chain, target, intensity, duration);
        return nextRange;
    }

    private bool IsInExclusionZone(EntityCoordinates target, List<Box2> exclusionZones)
    {
        var targetPos = target.Position;
        foreach (var zone in exclusionZones)
        {
            if (zone.Contains(targetPos))
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Spawns fire in a cone shape in the direction the entity is facing.
    /// </summary>
    private void SpawnFireCone(Entity<DirectionalTileFireOnTriggerComponent> ent, EntityCoordinates center, int? intensity = null, int? duration = null)
    {
        if (_net.IsClient) return;
        var chain = _onCollide.SpawnChain();
        ent.Comp.DiagonalRange = (int)Math.Floor((double)ent.Comp.Range / 2);
        Dirty(ent);

        var initialShot = !ent.Comp.InitialSpread;
        var target = center;
        var targets = new HashSet<EntityCoordinates> { };

        while (ent.Comp.Range > 0)
        {
            var shapeTargets = AddTarget(ent, target, initialShot);
            foreach (var coordinate in shapeTargets)
            {
                targets.Add(coordinate);
            }

            initialShot = false;
            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(target);
            while (anchored.MoveNext(out var uid))
            {
                if (_tag.HasAnyTag(uid, WallTag) &&
                    !_doorQuery.HasComp(uid))
                {
                    ent.Comp.Range = 0;
                    break;
                }
            }

            target = ChangeTarget(target, ent.Comp.Direction);
            ent.Comp.Range--;
            ent.Comp.DiagonalRange--;
        }

        foreach (var ignitionTarget in targets)
        {
            if (CheckViableTile(ent, ignitionTarget))
                SpawnFireChain(ent.Comp.Spawn, chain, ignitionTarget, intensity, duration);
        }
    }

    private EntityCoordinates ChangeTarget(EntityCoordinates target, Direction direction)
    {
        return target.Offset(direction);
    }

    /// <summary>
    ///     Returns targets based on the direction the entity that spawns the fires is facing.
    /// </summary>
    /// <param name="ent">The entity creating the fire</param>
    /// <param name="target">The tile the fire is being spawned from</param>
    /// <param name="direction">The direction the entity is facing</param>
    /// <param name="initialShot">If </param>
    /// <returns>Returns a list of potential targets for a fire to be spawned on</returns>
    private HashSet<EntityCoordinates> AddTarget(Entity<DirectionalTileFireOnTriggerComponent> ent, EntityCoordinates target, bool initialShot)
    {
        var targets = new HashSet<EntityCoordinates> { target };

        var width = ent.Comp.Width;
        var widthExtension = ent.Comp.Width + 1;
        var degrees = ent.Comp.Direction.ToAngle().Degrees;

        var centerTarget = target;
        var leftTarget = target;
        var rightTarget = target;

        while (width > 0)
        {
            //Logic to get the targets if the entity is facing an ordinal direction
            if ((int)degrees % 90 != 0)
            {
                while (widthExtension > 0 && ent.Comp.DiagonalRange > 0)
                {
                    centerTarget = ChangeTarget(centerTarget, ent.Comp.Direction);
                    leftTarget = ChangeTarget(leftTarget, Angle.FromDegrees(degrees - degrees % 90).GetDir());
                    rightTarget = ChangeTarget(rightTarget, Angle.FromDegrees(degrees + degrees % 90).GetDir());

                    targets.Add(leftTarget);
                    targets.Add(rightTarget);
                    targets.Add(centerTarget);

                    widthExtension--;
                }
            }
            else if (!initialShot)
            {
                leftTarget = ChangeTarget(leftTarget, Angle.FromDegrees(degrees - 90).GetDir());
                rightTarget = ChangeTarget(rightTarget, Angle.FromDegrees(degrees + 90).GetDir());
                targets.Add(leftTarget);
                targets.Add(rightTarget);
            }

            width--;
        }

        return targets;
    }

    /// <summary>
    ///     Checks if the targeted tile is viable for a fire to be spawned on and removes any existing fires from the tile.
    /// </summary>
    private bool CheckViableTile(Entity<DirectionalTileFireOnTriggerComponent> ent, EntityCoordinates target)
    {
        if (!_rmcMap.TryGetTileDef(target, out var tile) || tile.ID == ContentTileDefinition.SpaceID)
            return false;

        if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(target, out var oldTileFire))
        {
            QueueDel(oldTileFire);
        }

        return true;
    }

    private bool CanCraftMolotovPopup(Entity<CraftsIntoMolotovComponent> ent, EntityUid user, bool popup, out FixedPoint2 intensity)
    {
        intensity = default;
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            if (popup)
                _popup.PopupClient($"The {Name(ent)} is empty...", ent, user, PopupType.SmallCaution);

            return false;
        }

        intensity = FixedPoint2.Zero;
        foreach (var solutionReagent in solution)
        {
            if (!_prototype.TryIndex(solutionReagent.Reagent.Prototype, out ReagentPrototype? reagent))
                continue;

            intensity += reagent.Intensity * solutionReagent.Quantity;
        }

        if (intensity < ent.Comp.MinIntensity)
        {
            if (popup)
            {
                var msg = $"There's not enough flammable liquid in the {Name(ent)}!";
                _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            }

            return false;
        }

        intensity = FixedPoint2.Min(intensity, ent.Comp.MaxIntensity);
        return true;
    }

    private void OnPlasmaFrenzyIgnite(Entity<PlasmaFrenzyComponent> ent, ref RMCIgniteEvent args)
    {
        if (!TryComp<XenoPlasmaComponent>(ent, out var plasma))
            return;

        if (plasma.Plasma < plasma.MaxPlasma && _net.IsServer)
        {
            _emote.TryEmoteWithChat(ent, ent.Comp.RoarEmote);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-plasma-frenzy-fire"), ent, ent, PopupType.SmallCaution);
        }
        _plasma.SetPlasma((ent, plasma), plasma.MaxPlasma);
    }

    public void SetIntensityDuration(Entity<RMCIgniteOnCollideComponent?, DamageOnCollideComponent?> ent, int? intensity, int? duration)
    {
        Resolve(ent, ref ent.Comp1, ref ent.Comp2, false);
        if (ent.Comp1 != null)
        {
            if (intensity != null)
                ent.Comp1.Intensity = intensity.Value;

            if (duration != null)
                ent.Comp1.Duration = duration.Value;

            Dirty(ent, ent.Comp1);
        }

        if (ent.Comp2 != null)
        {
            if (duration != null)
                ent.Comp2.Damage.DamageDict[HeatDamage] = duration.Value;

            Dirty(ent, ent.Comp2);
        }
    }

    private void TryIgnite(Entity<RMCIgniteOnCollideComponent> ent, EntityUid other, bool checkIgnited)
    {
        var flammableEnt = new Entity<FlammableComponent?>(other, null);
        if (!Resolve(flammableEnt, ref flammableEnt.Comp, false))
            return;

        EnsureComp<SteppingOnFireComponent>(other);

        var wasOnFire = IsOnFire(flammableEnt);
        if (checkIgnited && wasOnFire)
            return;

        if (!Ignite(flammableEnt, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks))
            return;

        if (!wasOnFire && IsOnFire(flammableEnt) && !HasComp<RMCImmuneToFireTileDamageComponent>(ent))
            _damageable.TryChangeDamage(flammableEnt, flammableEnt.Comp.Damage * ent.Comp.Intensity, true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient) return;
        var time = _timing.CurTime;

        HandleOneTimeInitialization();
        UpdateTileFireVisuals(time);
        UpdateSteppingDamage(frameTime);

        _recheckAccumulator += frameTime;
        if (_recheckAccumulator >= RecheckInterval)
        {
            _recheckAccumulator = 0f;
            RecheckEntitiesOnFire();
        }

        _cleanupAccumulator += frameTime;
        if (_cleanupAccumulator >= CleanupInterval)
        {
            _cleanupAccumulator = 0f;
            CleanupDeletedEntities();
        }

        CleanupExtinguishComponents();
    }

    private void UpdateTileFireVisuals(TimeSpan time)
    {
        var tileFireQuery = EntityQueryEnumerator<TileFireComponent>();

        while (tileFireQuery.MoveNext(out var uid, out var fire))
        {
            var despawnAt = fire.SpawnedAt + fire.Duration;
            var timeLeft = despawnAt - time;

            if (timeLeft <= TimeSpan.Zero)
            {
                QueueDel(uid);
                continue;
            }

            TileFireVisuals visual;
            if (time < fire.SpawnedAt + fire.BigFireDuration) visual = TileFireVisuals.Four;
            else if (timeLeft < TimeSpan.FromSeconds(9)) visual = TileFireVisuals.One;
            else if (timeLeft < TimeSpan.FromSeconds(25)) visual = TileFireVisuals.Two;
            else visual = TileFireVisuals.Three;

            _appearance.SetData(uid, TileFireLayers.Base, visual);
        }
    }

    private void HandleOneTimeInitialization()
    {
        var applyQuery = EntityQueryEnumerator<RMCIgniteOnCollideComponent>();

        while (applyQuery.MoveNext(out var uid, out var apply))
        {
            if (apply.InitDamaged) continue;

            apply.InitDamaged = true;
            Dirty(uid, apply);
            RemCompDeferred<DamageOnCollideComponent>(uid);
        }
    }

    private void UpdateSteppingDamage(float frameTime)
    {
        _steppingAccumulator += frameTime;
        if (_steppingAccumulator < SteppingUpdateInterval) return;
        _steppingAccumulator = 0f;

        var steppingQuery = EntityQueryEnumerator<SteppingOnFireComponent>();

        while (steppingQuery.MoveNext(out var uid, out var stepping))
        {
            stepping.ArmorMultiplier = 1;
            var isStepping = false;

            foreach (var (fireEntity, contacts) in _fireContacts)
            {
                if (!contacts.Contains(uid)) continue;
                if (!_igniteOnCollideQuery.TryComp(fireEntity, out var ignite)) continue;
                if (ignite.TileDamage is not { } tile) continue;

                ProcessSteppingDamage(uid, stepping, ignite, tile);
                isStepping = true;
                break;
            }

            if (!isStepping)
            {
                RemCompDeferred<SteppingOnFireComponent>(uid);
            }
            else
            {
                Dirty(uid, stepping);
            }
        }
    }

    private void ProcessSteppingDamage(EntityUid uid, SteppingOnFireComponent stepping, RMCIgniteOnCollideComponent ignite, DamageSpecifier tile)
    {
        if (ignite.ArmorMultiplier < stepping.ArmorMultiplier && _entityWhitelist.IsWhitelistPassOrNull(ignite.ArmorWhitelist, uid))
        {
            stepping.ArmorMultiplier = ignite.ArmorMultiplier;
            if (TryComp<RMCFireArmorDebuffModifierComponent>(uid, out var mod))
            {
                stepping.ArmorMultiplier *= mod.DebuffModifier;
            }
            _armor.UpdateArmorValue((uid, null));
        }

        var coords = _transform.GetMoverCoordinates(uid);
        if (stepping.LastPosition is { } last && last.TryDistance(EntityManager, _transform, coords, out var distance))
        {
            stepping.Distance += distance;
            if (stepping.Distance >= 1)
            {
                stepping.Distance = 0;
                if (!HasComp<RMCImmuneToFireTileDamageComponent>(uid))
                {
                    var damage = tile * ignite.Intensity;
                    _damageable.TryChangeDamage(uid, damage);
                }
            }
        }

        Ignite(uid, ignite.Intensity, ignite.Duration, ignite.MaxStacks);
        stepping.LastPosition = coords;
    }

    private void RecheckEntitiesOnFire()
    {
        var toRemove = new HashSet<EntityUid>();

        foreach (var entity in _entitiesNeedingRecheck)
        {
            if (TerminatingOrDeleted(entity))
            {
                toRemove.Add(entity);
                continue;
            }

            var touchingFire = false;
            foreach (var (fireEntity, contacts) in _fireContacts)
            {
                if (contacts.Contains(entity))
                {
                    touchingFire = true;
                    if (_igniteOnCollideQuery.TryComp(fireEntity, out var ignite))
                    {
                        TryIgnite((fireEntity, ignite), entity, false);
                    }
                    break;
                }
            }

            if (!touchingFire)
            {
                toRemove.Add(entity);
                RemCompDeferred<SteppingOnFireComponent>(entity);
            }
        }

        foreach (var entity in toRemove)
            _entitiesNeedingRecheck.Remove(entity);
    }

    private void CleanupExtinguishComponents()
    {
        var extinguishQuery = EntityQueryEnumerator<ExtinguishFireComponent>();

        while (extinguishQuery.MoveNext(out var uid, out var extinguish))
        {
            if (extinguish.Extinguished) continue;

            extinguish.Extinguished = true;
            Dirty(uid, extinguish);
        }
    }

    private void CleanupDeletedEntity(EntityUid deletedEntity)
    {
        foreach (var (fireEntity, contacts) in _fireContacts.ToList())
        {
            contacts.Remove(deletedEntity);
            if (contacts.Count == 0)
            {
                _fireContacts.Remove(fireEntity);
            }
        }

        foreach (var (extinguishEntity, contacts) in _extinguishContacts.ToList())
        {
            contacts.Remove(deletedEntity);
            if (contacts.Count == 0)
            {
                _extinguishContacts.Remove(extinguishEntity);
            }
        }

        _entitiesNeedingRecheck.Remove(deletedEntity);
    }

    private void CleanupDeletedEntities()
    {
        var allTrackedEntities = new HashSet<EntityUid>();

        foreach (var contacts in _fireContacts.Values)
        {
            foreach (var entity in contacts)
                allTrackedEntities.Add(entity);
        }

        foreach (var contacts in _extinguishContacts.Values)
        {
            foreach (var entity in contacts)
                allTrackedEntities.Add(entity);
        }

        foreach (var entity in _entitiesNeedingRecheck)
            allTrackedEntities.Add(entity);

        foreach (var entity in allTrackedEntities)
        {
            if (TerminatingOrDeleted(entity))
            {
                CleanupDeletedEntity(entity);
            }
        }
    }
}
