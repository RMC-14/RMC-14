using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Neurotoxin;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Content.Shared.Throwing;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.CrashLand;

public abstract partial class SharedCrashLandSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    protected static readonly ProtoId<DamageTypePrototype> CrashLandDamageType = "Blunt";
    protected const int CrashLandDamageAmount = 10000;

    private bool _crashLandEnabled;

    private EntityQuery<CrashLandableComponent> _crashLandableQuery;

    private readonly EntProtoId<CrashLandingBlockedComponent> _crashLandingBlocker = "RMCCrashLandingBlocker";
    private readonly float _crashLandingBlockerRadius = 10;
    private readonly HashSet<Entity<CrashLandingBlockedComponent>> _crashLandingBlockers = new();

    public override void Initialize()
    {
        _crashLandableQuery = GetEntityQuery<CrashLandableComponent>();

        SubscribeLocalEvent<CrashLandableComponent, EntParentChangedMessage>(OnCrashLandableParentChanged);

        SubscribeLocalEvent<CrashLandOnTouchComponent, StartCollideEvent>(OnCrashLandOnTouchStartCollide);

        SubscribeLocalEvent<DeleteCrashLandableOnTouchComponent, StartCollideEvent>(OnDeleteCrashLandableOnTouchStartCollide);

        SubscribeLocalEvent<CrashLandingComponent, MapInitEvent>(OnCrashLandingMapInit);
        SubscribeLocalEvent<CrashLandingComponent, ComponentShutdown>(OnCrashLandingShutdown);
        SubscribeLocalEvent<CrashLandingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<CrashLandingComponent, VehicleCanRunEvent>(OnVehicleCanRun);
        SubscribeLocalEvent<CrashLandingComponent, RMCIgniteAttemptEvent>(OnIgniteAttempt);
        SubscribeLocalEvent<CrashLandingComponent, GettingAttackedAttemptEvent>(OnGettingAttacked);
        SubscribeLocalEvent<CrashLandingComponent, AttemptMobCollideEvent>(OnAttemptMobCollide);
        SubscribeLocalEvent<CrashLandingComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);
        SubscribeLocalEvent<CrashLandingComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        SubscribeLocalEvent<CrashLandingComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<CrashLandingComponent, NeurotoxinInjectAttemptEvent>(OnNeurotoxinInjectAttempt);

        Subs.CVar(_config, RMCCVars.RMCFTLCrashLand, v => _crashLandEnabled = v, true);
    }

    private void OnCrashLandableParentChanged(Entity<CrashLandableComponent> crashLandable, ref EntParentChangedMessage args)
    {
        if (!_crashLandEnabled || !HasComp<FTLMapComponent>(args.Transform.ParentUid))
            return;

        if (args.OldParent == null)
            return;

        // Try to crash any entities being pulled.
        if (TryComp(crashLandable, out PullerComponent? puller) &&
            puller.Pulling != null &&
            _crashLandableQuery.TryComp(puller.Pulling.Value, out var pullingCrashLandable) &&
            ShouldCrash(puller.Pulling.Value, args.OldParent.Value))
        {
            TryCrashLand((puller.Pulling.Value, pullingCrashLandable), true);
        }

        if (!ShouldCrash(crashLandable, args.OldParent.Value))
            return;

        TryCrashLand(crashLandable.Owner, true);
    }

    private void OnCrashLandOnTouchStartCollide(Entity<CrashLandOnTouchComponent> ent, ref StartCollideEvent args)
    {
        if (!_crashLandEnabled || !_crashLandableQuery.TryGetComponent(args.OtherEntity, out var crashLandable))
            return;

        var ev = new AttemptCrashLandEvent(args.OtherEntity);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return;

        TryCrashLand((args.OtherEntity, crashLandable), true);
    }

    private void OnDeleteCrashLandableOnTouchStartCollide(Entity<DeleteCrashLandableOnTouchComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_crashLandEnabled || !_crashLandableQuery.HasComp(args.OtherEntity))
            return;

        QueueDel(args.OtherEntity);
    }

    private void OnCrashLandingMapInit(Entity<CrashLandingComponent> ent, ref MapInitEvent args)
    {
        DisableFallingCollisions(ent, ent.Comp.OriginalLayers, ent.Comp.OriginalMasks);
        Dirty(ent);
    }

    private void OnCrashLandingShutdown(Entity<CrashLandingComponent> ent, ref ComponentShutdown args)
    {
        RestoreFallingCollisions(ent, ent.Comp.OriginalLayers, ent.Comp.OriginalMasks);
    }

    public void DisableFallingCollisions(EntityUid entity, Dictionary<string, int> originalLayers, Dictionary<string, int> originalMasks)
    {
        if (!HasComp<PhysicsComponent>(entity) || !TryComp(entity, out FixturesComponent? fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures)
        {
            originalLayers.TryAdd(fixture.Key, fixture.Value.CollisionLayer);
            originalMasks.TryAdd(fixture.Key, fixture.Value.CollisionMask);

            _physics.SetCollisionLayer(entity, fixture.Key, fixture.Value, (int) CollisionGroup.None);
            _physics.SetCollisionMask(entity, fixture.Key, fixture.Value, (int) CollisionGroup.None);
        }
    }

    public void RestoreFallingCollisions(
        EntityUid entity,
        IReadOnlyDictionary<string, int> originalLayers,
        IReadOnlyDictionary<string, int> originalMasks)
    {
        if (!HasComp<PhysicsComponent>(entity) || !TryComp(entity, out FixturesComponent? fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures)
        {
            if (!originalLayers.TryGetValue(fixture.Key, out var originalLayer) ||
                !originalMasks.TryGetValue(fixture.Key, out var originalMask))
                continue;

            _physics.SetCollisionLayer(entity, fixture.Key, fixture.Value, originalLayer);
            _physics.SetCollisionMask(entity, fixture.Key, fixture.Value, originalMask);
        }
    }

    private void OnUpdateCanMove(Entity<CrashLandingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnVehicleCanRun(Entity<CrashLandingComponent> ent, ref VehicleCanRunEvent args)
    {
        args.CanRun = false;
    }

    private void OnIgniteAttempt(Entity<CrashLandingComponent> ent, ref RMCIgniteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttemptMobCollide(Entity<CrashLandingComponent> ent, ref AttemptMobCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAttemptMobTargetCollide(Entity<CrashLandingComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGettingAttacked(Entity<CrashLandingComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnThrowPushbackAttempt(Entity<CrashLandingComponent> ent, ref ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeDamageChanged(Entity<CrashLandingComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnNeurotoxinInjectAttempt(Entity<CrashLandingComponent> ent, ref NeurotoxinInjectAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private bool ShouldCrash(EntityUid crashing, EntityUid oldParent)
    {
        var ev = new AttemptCrashLandEvent(crashing);
        RaiseLocalEvent(oldParent, ref ev);

        if (ev.Cancelled)
            return false;

        return true;
    }

    public void ApplyFallingDamage(EntityUid uid)
    {
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                [CrashLandDamageType] = CrashLandDamageAmount,
            },
        };

        Damageable.TryChangeDamage(uid, damage);
    }

    public bool IsLandableTile(Entity<MapGridComponent> grid, TileRef tileRef)
    {
        return IsLandableTile(grid, tileRef, false);
    }

    public bool IsLandableTile(Entity<MapGridComponent> grid, TileRef tileRef, bool ignoreParadropRestrictions)
    {
        var tile = tileRef.GridIndices;
        var location = _mapSystem.GridTileToLocal(grid, grid, tile);

        if (_turf.GetContentTileDefinition(tileRef).ID == ContentTileDefinition.SpaceID)
            return false;

        // no air-blocked areas.
        if (_turf.IsSpace(tileRef) ||
            _turf.IsTileBlocked(tileRef, CollisionGroup.MobMask))
        {
            return false;
        }

        if (!ignoreParadropRestrictions && !_area.CanParadrop(location))
            return false;

        // don't spawn inside of solid objects
        var physQuery = GetEntityQuery<PhysicsComponent>();
        var valid = true;

        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (anchored.MoveNext(out var ent))
        {
            if (!physQuery.TryGetComponent(ent, out var body))
                continue;

            if (body.BodyType != BodyType.Static ||
                !body.Hard ||
                (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                continue;

            valid = false;
            break;
        }

        return valid;
    }

    public bool TryGetLandableFootprint(EntityUid landing, Entity<MapGridComponent> grid, Vector2i center, bool ignoreParadropRestrictions, out List<TileRef> footprint)
    {
        footprint = GetLandingFootprint(landing, grid, center);
        foreach (var tile in footprint)
        {
            if (!IsLandableTile(grid, tile, ignoreParadropRestrictions))
                return false;
        }

        return footprint.Count > 0;
    }

    public List<TileRef> GetLandingFootprint(EntityUid landing, Entity<MapGridComponent> grid, Vector2i center)
    {
        var centerCoordinates = _mapSystem.GridTileToLocal(grid, grid, center);
        var rotation = _transform.GetWorldRotation(landing) - _transform.GetWorldRotation(grid);
        var bounds = _entityLookup.GetAABBNoContainer(landing, centerCoordinates.Position, rotation);

        // Prevent exact 1 tile sized fixtures from putting warnings on the neighboring tiles.
        var horizontalInset = MathF.Min(PhysicsConstants.PolygonRadius, bounds.Width / 2);
        var verticalInset = MathF.Min(PhysicsConstants.PolygonRadius, bounds.Height / 2);
        var footprintBounds = new Box2(
            bounds.Left + horizontalInset,
            bounds.Bottom + verticalInset,
            bounds.Right - horizontalInset,
            bounds.Top - verticalInset);
        var tileBounds = Box2.CenteredAround(centerCoordinates.Position, new Vector2(grid.Comp.TileSize));
        if (tileBounds.Enlarged(PhysicsConstants.LinearSlop).Contains(footprintBounds))
        {
            var centerFootprint = new List<TileRef>();
            if (_mapSystem.TryGetTileRef(grid, grid, center, out var centerTile))
                centerFootprint.Add(centerTile);

            return centerFootprint;
        }

        var footprint = _mapSystem.GetLocalTilesIntersecting(grid, grid, footprintBounds, false).ToList();
        if (footprint.Count == 0 && _mapSystem.TryGetTileRef(grid, grid, center, out var fallbackTile))
            footprint.Add(fallbackTile);

        return footprint;
    }

    /// <summary>
    /// Try and get a valid position to crash land on.
    /// Used for blind para-dropping and failed evacuation pods/shuttles.
    /// </summary>
    /// <param name="blocking">Is the thing crashing a grid (evacuation pod/shuttle)?</param>
    /// <param name="location"></param>
    /// <returns>True if a valid location has been found.</returns>
    public bool TryGetCrashLandLocation(bool blocking, out EntityCoordinates location)
    {
        return TryGetCrashLandLocation(null, blocking, out location);
    }

    public bool TryGetCrashLandLocation(EntityUid landing, bool blocking, out EntityCoordinates location)
    {
        return TryGetCrashLandLocation((EntityUid?) landing, blocking, out location);
    }

    private bool TryGetCrashLandLocation(EntityUid? landing, bool blocking, out EntityCoordinates location)
    {
        location = default;
        var distressQuery = EntityQueryEnumerator<RMCPlanetComponent>();
        while (distressQuery.MoveNext(out var grid, out _))
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                return false;

            var xform = Transform(grid);
            location = xform.Coordinates;
            for (var i = 0; i < 250; i++)
            {
                // TODO RMC14 every single method used in content and engine for "random spot" is broken with planet maps. Splendid!
                var randomX = _random.Next(-200, 200);
                var randomY = _random.Next(-200, 200);
                var tile = new Vector2i(randomX, randomY);
                if (!_mapSystem.TryGetTileRef(grid, gridComp, tile, out var tileRef))
                    continue;

                if (landing is { } entity)
                {
                    if (!TryGetLandableFootprint(entity, (grid, gridComp), tile, false, out _))
                        continue;
                }
                else if (!IsLandableTile((grid, gridComp), tileRef))
                    continue;

                location = _mapSystem.GridTileToLocal(grid, gridComp, tile);

                if (!blocking)
                    return true;

                _crashLandingBlockers.Clear();
                _entityLookup.GetEntitiesInRange(location, _crashLandingBlockerRadius, _crashLandingBlockers);
                if (_crashLandingBlockers.Count > 0)
                    continue;

                SpawnAtPosition(_crashLandingBlocker, location);

                return true;
            }
        }

        return false;
    }

    public void TryCrashLand(Entity<CrashLandableComponent?> crashLandable, bool doDamage)
    {
        if (_net.IsClient)
            return;

        if (!TryGetCrashLandLocation(crashLandable, false, out var location))
            return;

        TryCrashLand(crashLandable.Owner, doDamage, location);
    }

    public void TryCrashLand(Entity<CrashLandableComponent?> crashLandable, bool doDamage, EntityCoordinates location)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(crashLandable, ref crashLandable.Comp, false))
            return;

        if (HasComp<CrashLandingComponent>(crashLandable))
            return;

        var skyFalling = EnsureComp<SkyFallingComponent>(crashLandable);
        skyFalling.RemainingTime = crashLandable.Comp.SkyFallDuration;
        skyFalling.TargetCoordinates = location;
        skyFalling.DropSound = crashLandable.Comp.DropSound;
        Dirty(crashLandable, skyFalling);

        var crashLanding = EnsureComp<CrashLandingComponent>(crashLandable);
        crashLanding.DoDamage = doDamage;
        crashLanding.RemainingTime = crashLandable.Comp.CrashDuration;
        Dirty(crashLandable, crashLanding);

        Blocker.UpdateCanMove(crashLandable);

        crashLandable.Comp.LastCrash = _timing.CurTime;
        Dirty(crashLandable);

        _rmcPulling.TryStopAllPullsFromAndOn(crashLandable);

        var ev = new CrashLandStartedEvent();
        RaiseLocalEvent(crashLandable, ref ev);
    }

    public void DoCrashLand(EntityUid crashing, EntityCoordinates crashLocation, float skyFallDuration = 1.5f, float crashDuration = 0.75f, bool doDamage = true, SoundSpecifier? dropSound = null, SoundSpecifier? crashSound = null)
    {
        if (!EnsureComp<CrashLandableComponent>(crashing, out var crashLandable))
            crashLandable.RemoveComponentAfterCrash = true;

        crashLandable.CrashSound = crashSound;
        crashLandable.SkyFallDuration = skyFallDuration;
        crashLandable.CrashDuration = crashDuration;
        crashLandable.DropSound = dropSound;
        Dirty(crashing, crashLandable);

        TryCrashLand(crashing, doDamage, crashLocation);
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var crashLandingQuery = EntityQueryEnumerator<CrashLandableComponent, CrashLandingComponent>();
        while (crashLandingQuery.MoveNext(out var uid, out var crashLandable, out var crashLanding))
        {
            if (HasComp<SkyFallingComponent>(uid))
                continue;

            crashLanding.RemainingTime -= frameTime;
            if (!(crashLanding.RemainingTime <= 0))
                continue;

            var ev = new CrashLandedEvent(crashLanding.DoDamage);
            RaiseLocalEvent(uid, ref ev);

            if (_net.IsServer)
                _audio.PlayPvs(crashLandable.CrashSound, uid);

            RemComp<CrashLandingComponent>(uid);

            if (crashLanding.DoDamage)
                ApplyFallingDamage(uid);

            if (crashLandable.RemoveComponentAfterCrash)
                RemCompDeferred<CrashLandableComponent>(uid);

            Blocker.UpdateCanMove(uid);
        }
    }
}

[ByRefEvent]
public record struct AttemptCrashLandEvent(EntityUid Crashing, EntityCoordinates? Target = null, bool Cancelled = false);

[ByRefEvent]
public record struct CrashLandStartedEvent;

[ByRefEvent]
public record struct CrashLandedEvent(bool ShouldDamage);

[Serializable, NetSerializable]
public abstract class FallAnimationEventArgs : EntityEventArgs
{
    public NetEntity Entity;
    public NetCoordinates Coordinates;
    public float FallDuration;
}

[Serializable, NetSerializable]
public abstract class CrashAnimationMsg : FallAnimationEventArgs
{

}
