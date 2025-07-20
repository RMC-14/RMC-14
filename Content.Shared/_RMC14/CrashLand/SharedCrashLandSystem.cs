using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Maps;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.ParaDrop;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
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
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    protected static readonly ProtoId<DamageTypePrototype> CrashLandDamageType = "Blunt";
    protected const int CrashLandDamageAmount = 10000;

    private bool _crashLandEnabled;

    private EntityQuery<CrashLandableComponent> _crashLandableQuery;

    public override void Initialize()
    {
        _crashLandableQuery = GetEntityQuery<CrashLandableComponent>();

        SubscribeLocalEvent<CrashLandableComponent, EntParentChangedMessage>(OnCrashLandableParentChanged);

        SubscribeLocalEvent<CrashLandOnTouchComponent, StartCollideEvent>(OnCrashLandOnTouchStartCollide);

        SubscribeLocalEvent<DeleteCrashLandableOnTouchComponent, StartCollideEvent>(OnDeleteCrashLandableOnTouchStartCollide);

        SubscribeLocalEvent<CrashLandingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);

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

    private void OnUpdateCanMove(Entity<CrashLandingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
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

        if (!_area.CanCAS(location) ||
            !_area.CanFulton(location) ||
            !_area.CanSupplyDrop(_transform.ToMapCoordinates(location)))
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

    public bool TryGetCrashLandLocation(out EntityCoordinates location)
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

                if (!IsLandableTile((grid, gridComp), tileRef))
                    continue;

                location = _mapSystem.GridTileToLocal(grid, gridComp, tile);
                return true;
            }
        }

        return false;
    }

    public void TryCrashLand(Entity<CrashLandableComponent?> crashLandable, bool doDamage)
    {
        if (_net.IsClient)
            return;

        if (!TryGetCrashLandLocation(out var location))
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
        skyFalling.TargetCoordinates = location;
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

            if (crashLanding.DoDamage)
                ApplyFallingDamage(uid);

            var ev = new CrashLandedEvent(crashLanding.DoDamage);
            RaiseLocalEvent(uid, ref ev);

            if (_net.IsServer)
                _audio.PlayPvs(crashLandable.CrashSound, uid);

            RemComp<CrashLandingComponent>(uid);
            Blocker.UpdateCanMove(uid);
        }
    }
}

[ByRefEvent]
public record struct AttemptCrashLandEvent(EntityUid Crashing, bool Cancelled = false);

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
