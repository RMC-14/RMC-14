using System.Numerics;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Slow;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ParaDrop;

public abstract partial class SharedParaDropSystem : EntitySystem
{
    [Dependency] private readonly CrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected static readonly Vector2 ParachuteEffectOffset = new (0, 0.75f);

    private static readonly int CrashScatter = 4;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParaDropOnTouchComponent, AttemptCrashLandEvent>(OnAttemptCrashLand);

        SubscribeLocalEvent<GrantParaDroppableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantParaDroppableComponent, GotUnequippedEvent>(OnGotUnEquipped);
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

    private void OnAttemptCrashLand(Entity<ParaDropOnTouchComponent> ent, ref AttemptCrashLandEvent args)
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropShip))
            return;

        if (!TryComp(dropShip, out ActiveParaDropComponent? paraDrop) &&
            !HasComp<ParaDroppableComponent>(args.Crashing))
            return;

        args.Cancelled = true;

        AttemptParaDrop((dropShip, paraDrop), args.Crashing);
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

        // This is to prevent ghost parachutes
        if (paraDroppable.LastParaDrop != null && paraDroppable.LastParaDrop.Value + paraDroppable.ParaDropCooldown > _timing.CurTime)
            return false;

        paraDroppable.LastParaDrop = _timing.CurTime;
        Dirty(dropping, paraDroppable);

        _slow.TryRoot(dropping, TimeSpan.FromSeconds(paraDroppable.DropDuration));
        if (TryComp(dropping, out PhysicsComponent? physics))
        {
            _physics.SetLinearVelocity(dropping, Vector2.Zero, body: physics);
        }

        // Paradrop near the target location.
        if (TryGetParaDropLocation(dropCoordinates, paraDroppable.DropScatter, out var adjustedCoordinates))
            dropCoordinates = adjustedCoordinates;

        _transform.SetMapCoordinates(dropping, _transform.ToMapCoordinates(dropCoordinates));
        _audio.PlayPvs(paraDroppable.DropSound, dropping);

        var ev = new ParaDropAnimationMessage
        {
            Entity = GetNetEntity(dropping),
            Coordinates = GetNetCoordinates(dropCoordinates),
            FallDuration = paraDroppable.DropDuration,
            ParachuteSprite = paraDroppable.ParachuteSprite,
        };
        RaiseNetworkEvent(ev);

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

            var random = _random.Next(0, viableTiles.Count);
            adjustedLocation = _mapSystem.GridTileToLocal(grid, gridComp, viableTiles[random].GridIndices);
            return true;
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveParaDropComponent, DropshipComponent>();

        while (query.MoveNext(out var uid, out var paraDrop, out var dropship))
        {
            // Stop targeting when the target disappears, or when the dropships starts it's landing procedures.
            if (dropship.State == FTLState.Arriving || !HasComp<DropshipTargetComponent>(paraDrop.DropTarget))
                RemCompDeferred<ActiveParaDropComponent>(uid);
        }
    }
}

[Serializable, NetSerializable]
public sealed class ParaDropAnimationMessage : FallAnimationEventArgs
{
    public SpriteSpecifier ParachuteSprite = new SpriteSpecifier.Rsi(new ResPath("Objects/Tools/fulton_balloon.rsi"), "fulton_balloon");
}
