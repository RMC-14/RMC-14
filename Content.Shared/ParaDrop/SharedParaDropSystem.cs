using System.Numerics;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Neurotoxin;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ParaDrop;

public abstract partial class SharedParaDropSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int CrashScatter = 7;

    public override void Initialize()
    {
        SubscribeLocalEvent<CrashLandOnTouchComponent, AttemptCrashLandEvent>(OnAttemptCrashLand);
        SubscribeLocalEvent<MapGridComponent, AttemptCrashLandEvent>(OnAttemptCrashLand);

        SubscribeLocalEvent<GrantParaDroppableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantParaDroppableComponent, GotUnequippedEvent>(OnGotUnEquipped);

        SubscribeLocalEvent<ParaDroppingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ParaDroppingComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ParaDroppingComponent, RMCIgniteAttemptEvent>(OnIgniteAttempt);
        SubscribeLocalEvent<ParaDroppingComponent, GettingAttackedAttemptEvent>(OnGettingAttacked);
        SubscribeLocalEvent<ParaDroppingComponent, AttemptMobCollideEvent>(OnAttemptMobCollide);
        SubscribeLocalEvent<ParaDroppingComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);
        SubscribeLocalEvent<ParaDroppingComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        SubscribeLocalEvent<ParaDroppingComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<ParaDroppingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ParaDroppingComponent, NeurotoxinInjectAttemptEvent>(OnNeurotoxinInjectAttempt);

        SubscribeLocalEvent<SkyFallingComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnGotEquipped(Entity<GrantParaDroppableComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        EnsureComp<ParaDroppableComponent>(args.Equipee);
    }

    private void OnGotUnEquipped(Entity<GrantParaDroppableComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        RemComp<ParaDroppableComponent>(args.Equipee);
    }

    private void OnAttemptCrashLand(Entity<CrashLandOnTouchComponent> ent, ref AttemptCrashLandEvent args)
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropShip))
            return;

        if (!TryComp(dropShip, out ActiveParaDropComponent? paraDrop) &&
            !HasComp<ParaDroppableComponent>(args.Crashing))
            return;

        args.Cancelled = true;

        AttemptParaDrop((dropShip, paraDrop), args.Crashing);
    }

    private void OnAttemptCrashLand(Entity<MapGridComponent> ent, ref AttemptCrashLandEvent args)
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropShip))
            return;

        if (!TryComp(dropShip, out ActiveParaDropComponent? paraDrop) &&
            !HasComp<ParaDroppableComponent>(args.Crashing))
            return;

        args.Cancelled = true;

        AttemptParaDrop((dropShip, paraDrop), args.Crashing);
    }

    private void OnMapInit(Entity<ParaDroppingComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out PhysicsComponent? physics) || !TryComp(ent, out FixturesComponent? fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures)
        {
            ent.Comp.OriginalLayers.TryAdd(fixture.Key, fixture.Value.CollisionLayer);
            ent.Comp.OriginalMasks.TryAdd(fixture.Key, fixture.Value.CollisionMask);

            _physics.SetCollisionLayer(ent, fixture.Key, fixture.Value, (int) CollisionGroup.None);
            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, (int) CollisionGroup.None);
        }

        Dirty(ent);
    }

    private void OnComponentShutdown(Entity<ParaDroppingComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out PhysicsComponent? physics) || !TryComp(ent, out FixturesComponent? fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures)
        {
            if (!ent.Comp.OriginalLayers.TryGetValue(fixture.Key, out var originalLayer) ||
                !ent.Comp.OriginalMasks.TryGetValue(fixture.Key, out var originalMask))
                continue;

            _physics.SetCollisionLayer(ent, fixture.Key, fixture.Value, originalLayer);
            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, originalMask);
        }
    }

    private void OnComponentShutdown(Entity<SkyFallingComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.TargetCoordinates == null)
            return;

        _transform.SetMapCoordinates(ent, _transform.ToMapCoordinates(ent.Comp.TargetCoordinates.Value));

        if (TryComp(ent, out ParaDroppableComponent? paraDroppable))
            _audio.PlayPvs(paraDroppable.DropSound, ent);
    }

    private void OnIgniteAttempt(Entity<ParaDroppingComponent> ent, ref RMCIgniteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttemptMobCollide(Entity<ParaDroppingComponent> ent, ref AttemptMobCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAttemptMobTargetCollide(Entity<ParaDroppingComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGettingAttacked(Entity<ParaDroppingComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnThrowPushbackAttempt(Entity<ParaDroppingComponent> ent, ref ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeDamageChanged(Entity<ParaDroppingComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUpdateCanMove(Entity<ParaDroppingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnNeurotoxinInjectAttempt(Entity<ParaDroppingComponent> ent, ref NeurotoxinInjectAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    ///     Try to do a paradrop, if the dropShip has no <see cref="ActiveParaDropComponent"/> the drop location will be random.
    /// </summary>
    /// <param name="dropShip">The entity that decides the target of the drop</param>
    /// <param name="dropping">The entity that is trying to paradrop</param>
    private void AttemptParaDrop(Entity<ActiveParaDropComponent?> dropShip, EntityUid dropping)
    {
        if (_net.IsClient)
            return;

        if (HasComp<ParaDroppingComponent>(dropping))
            return;

        EntityUid? dropTarget = null;

        if (dropShip.Comp?.DropTarget != null)
            dropTarget = dropShip.Comp.DropTarget;

        // Drop at a random location.
        if (dropTarget == null)
        {
            EntityCoordinates? randomCoordinates = null;
            if (_crashLand.TryGetCrashLandLocation(out var location))
                randomCoordinates = location;

            // Cancel the jump if there is no viable target
            if (randomCoordinates == null)
            {
                _popup.PopupClient("Your harness got stuck and is preventing your from jumping", dropping, PopupType.SmallCaution);
                return;
            }

            TryDrop(dropping, randomCoordinates.Value);

            return;
        }

        TryDrop(dropping, _transform.GetMoverCoordinates(dropTarget.Value));
    }

    /// <summary>
    ///     Try to safely drop the target on the target coordinates
    /// </summary>
    /// <param name="dropping">The paradropping entity</param>
    /// <param name="dropCoordinates">The coordinates the entity is being dropped at</param>
    /// <returns>True if the paradrop succeeded</returns>
    private bool TryDrop(EntityUid dropping, EntityCoordinates dropCoordinates)
    {
        // Try crashing near the target location
        if (!TryComp(dropping, out ParaDroppableComponent? paraDroppable))
        {
            if (TryGetParaDropLocation(dropCoordinates, CrashScatter, out var adjustedCrashCoordinates))
                dropCoordinates = adjustedCrashCoordinates;

            _crashLand.TryCrashLand(dropping, true, dropCoordinates);
            return false;
        }

        paraDroppable.LastParaDrop = _timing.CurTime;
        Dirty(dropping, paraDroppable);

        _rmcPulling.TryStopAllPullsFromAndOn(dropping);
        if (TryComp(dropping, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(dropping, Vector2.Zero, body: physics);

        // Paradrop near the target location.
        if (TryGetParaDropLocation(dropCoordinates, paraDroppable.DropScatter, out var adjustedCoordinates))
            dropCoordinates = adjustedCoordinates;

        var skyFalling = EnsureComp<SkyFallingComponent>(dropping);
        skyFalling.TargetCoordinates = dropCoordinates;
        Dirty(dropping, skyFalling);

        var droppingComp = EnsureComp<ParaDroppingComponent>(dropping);
        droppingComp.RemainingTime = paraDroppable.DropDuration;
        Dirty(dropping, droppingComp);

        Blocker.UpdateCanMove(dropping);

        return true;
    }

    /// <summary>
    ///     Get a new location near the target that can be paradropped to.
    /// </summary>
    /// <param name="targetLocation">The paradrop target location.</param>
    /// <param name="dropScatter">The maximum distance from the target location that can be dropped on</param>
    /// <param name="adjustedLocation">The new location after the scatter was applied</param>
    /// <returns></returns>
    private bool TryGetParaDropLocation(EntityCoordinates targetLocation, int dropScatter, out EntityCoordinates adjustedLocation)
    {
        adjustedLocation = default;
        var distressQuery = EntityQueryEnumerator<RMCPlanetComponent>();
        while (distressQuery.MoveNext(out var grid, out _))
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                return false;

            var position = _mapSystem.LocalToTile(grid, gridComp, targetLocation);
            var dropArea = new Box2(position.X - dropScatter, position.Y - dropScatter, position.X + dropScatter, position.Y + dropScatter);
            var enumerable = _mapSystem.GetTilesEnumerator(grid, gridComp, dropArea);

            var viableTiles = new List<TileRef>();
            while (enumerable.MoveNext(out var tileRef))
            {
                if (!_crashLand.IsLandableTile((grid, gridComp), tileRef))
                    continue;

                viableTiles.Add(tileRef);
            }

            if (viableTiles.Count == 0)
                return false;

            var random = _random.Next(0, viableTiles.Count);
            adjustedLocation = _mapSystem.GridTileToLocal(grid, gridComp, viableTiles[random].GridIndices);
            return true;
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        var dropshipQuery = EntityQueryEnumerator<ActiveParaDropComponent, DropshipComponent>();

        while (dropshipQuery.MoveNext(out var uid, out var paraDrop, out var dropship))
        {
            // Stop targeting when the target disappears, or when the dropships starts it's landing procedures.
            if (dropship.State == FTLState.Arriving || !HasComp<DropshipTargetComponent>(paraDrop.DropTarget))
                RemComp<ActiveParaDropComponent>(uid);
        }

        if (!_timing.IsFirstTimePredicted)
            return;

        var paraDroppingQuery = EntityQueryEnumerator<ParaDroppingComponent>();
        while (paraDroppingQuery.MoveNext(out var uid, out var paraDropping))
        {
            if (HasComp<SkyFallingComponent>(uid))
                continue;

            paraDropping.RemainingTime -= frameTime;
            if (paraDropping.RemainingTime <= 0)
                RemComp<ParaDroppingComponent>(uid);

            Blocker.UpdateCanMove(uid);
        }

        var skyFallingQuery = EntityQueryEnumerator<SkyFallingComponent>();
        while (skyFallingQuery.MoveNext(out var uid, out var skyFalling))
        {
            skyFalling.RemainingTime -= frameTime;
            if (skyFalling.RemainingTime <= 0)
                RemComp<SkyFallingComponent>(uid);
        }
    }
}
