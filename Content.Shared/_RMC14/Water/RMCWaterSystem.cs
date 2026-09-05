using System.Numerics;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Water;

public sealed class RMCWaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly RMCPullingSystem _pulling = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VehicleWheelSystem _vehicleWheels = default!;
    [Dependency] private readonly WeedKillerSystem _weedKiller = default!;

    private static readonly SoundSpecifier ToxicWaterSound = new SoundCollectionSpecifier("ToxicWaterSizzle");

    private static readonly Vector2i[] NeighborOffsets =
    [
        new(-1, -1),
        new(0, -1),
        new(1, -1),
        new(-1, 0),
        new(1, 0),
        new(-1, 1),
        new(0, 1),
        new(1, 1),
    ];

    private readonly List<WaterActivation> _pendingActivations = new();
    private readonly HashSet<Entity<ItemComponent>> _items = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PurifiableWaterComponent, MapInitEvent>(OnPurifiableWaterMapInit);
        SubscribeLocalEvent<PurifiableWaterComponent, RefreshNameModifiersEvent>(OnPurifiableWaterRefreshNameModifiers);
        SubscribeLocalEvent<WaterFilterComponent, MapInitEvent>(OnWaterFilterMapInit);
        SubscribeLocalEvent<WaterFilterComponent, ActivateInWorldEvent>(OnWaterFilterActivate);
        SubscribeLocalEvent<ToxicWaterComponent, StartCollideEvent>(OnToxicWaterStartCollide);
    }

    private void OnPurifiableWaterMapInit(Entity<PurifiableWaterComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);

        if (_net.IsServer && ent.Comp.State == PurifiableWaterState.Purified)
            RemCompDeferred<DamageOverTimeComponent>(ent);
    }

    private void OnPurifiableWaterRefreshNameModifiers(Entity<PurifiableWaterComponent> ent, ref RefreshNameModifiersEvent args)
    {
        var loc = ent.Comp.State == PurifiableWaterState.Purified
            ? "rmc-water-purified-name"
            : "rmc-water-toxic-name";
        args.AddModifier(loc);
    }

    private void OnWaterFilterMapInit(Entity<WaterFilterComponent> ent, ref MapInitEvent args)
    {
        UpdateFilterAppearance(ent);
    }

    private void OnWaterFilterActivate(Entity<WaterFilterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_net.IsClient || args.Handled || HasComp<XenoComponent>(args.User))
            return;

        args.Handled = true;

        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-machines-unpowered"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Active || !TryComp(ent, out WaterLinkComponent? link) || string.IsNullOrWhiteSpace(link.Id))
            return;

        var time = _timing.CurTime;
        ent.Comp.Active = true;
        ent.Comp.Triggered = false;
        ent.Comp.TriggerAt = time + ent.Comp.TriggerDelay;
        ent.Comp.ResetAt = time + ent.Comp.ResetDelay;
        Dirty(ent);

        _power.SetReceiverMode(ent.Owner, RMCPowerMode.Active);
        _power.TryUseOneOffPower(ent, ent.Comp.OneOffLoad);
        UpdateFilterAppearance(ent);
    }

    private void OnToxicWaterStartCollide(Entity<ToxicWaterComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient ||
            !HasComp<XenoComponent>(args.OtherEntity) ||
            !IsHazardous(ent) ||
            !IsActiveWater(ent.Owner, args.OtherEntity))
        {
            return;
        }

        if (!_pulling.TryStopPullFrom(args.OtherEntity, out var pulled))
            return;

        _popup.PopupClient(
            Loc.GetString("rmc-water-toxic-pull", ("target", pulled.Value)),
            args.OtherEntity,
            args.OtherEntity,
            PopupType.SmallCaution);
    }

    private void UpdateAppearance(Entity<PurifiableWaterComponent> ent)
    {
        var visual = ent.Comp.State switch
        {
            PurifiableWaterState.Toxic => PurifiableWaterVisuals.Toxic,
            PurifiableWaterState.Dispersing => PurifiableWaterVisuals.Dispersing,
            PurifiableWaterState.Purified => PurifiableWaterVisuals.Purified,
            _ => throw new ArgumentOutOfRangeException(),
        };

        _appearance.SetData(ent.Owner, PurifiableWaterLayers.Layer, visual);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void UpdateFilterAppearance(Entity<WaterFilterComponent> ent)
    {
        _appearance.SetData(ent.Owner, WaterFilterVisuals.Active, ent.Comp.Active);
    }

    public bool CanCollide(Entity<RMCWaterComponent?> water, EntityUid user)
    {
        if (!Resolve(water, ref water.Comp, false))
            return true;

        if (water.Comp.Cover is not { } cover)
            return true;

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(water);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (_entityWhitelist.IsWhitelistPass(cover, anchoredId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true for uncovered RMC water that should apply water contact effects.
    /// </summary>
    public bool IsActiveWater(EntityUid water, EntityUid user, RMCWaterComponent? component = null)
    {
        return IsActiveWater((water, component), user);
    }

    public bool IsActiveWater(Entity<RMCWaterComponent?> water, EntityUid user)
    {
        if (!Resolve(water, ref water.Comp, false))
            return false;

        return CanCollide(water, user);
    }

    /// <summary>
    /// Checks current physics contacts for active RMC water.
    /// </summary>
    public bool IsInWater(EntityUid user, FixturesComponent? fixtures = null)
    {
        if (!Resolve(user, ref fixtures, false))
            return false;

        var contacts = _physics.GetContacts((user, fixtures));
        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            if (IsActiveWater(contact.OtherEnt(user), user))
                return true;
        }

        return false;
    }

    private bool IsHazardous(EntityUid water)
    {
        return !TryComp(water, out PurifiableWaterComponent? purifiable) ||
               purifiable.State != PurifiableWaterState.Purified;
    }

    public bool StartPurification(Entity<PurifiableWaterComponent?> water)
    {
        if (_net.IsClient ||
            !Resolve(water, ref water.Comp, false) ||
            water.Comp.State != PurifiableWaterState.Toxic ||
            HasComp<ActiveWaterComponent>(water))
        {
            return false;
        }

        ScheduleActivation((water.Owner, water.Comp), _timing.CurTime, Vector2.Zero);
        return true;
    }

    private void TriggerFilter(Entity<WaterFilterComponent> filter)
    {
        if (!TryComp(filter, out WaterLinkComponent? filterLink) ||
            string.IsNullOrWhiteSpace(filterLink.Id))
        {
            return;
        }

        var filterMap = Transform(filter).MapID;
        var initiators = EntityQueryEnumerator<WaterFilterInitiatorComponent, WaterLinkComponent, TransformComponent>();
        while (initiators.MoveNext(out var initiator, out _, out var initiatorLink, out var initiatorXform))
        {
            if (initiatorXform.MapID != filterMap || initiatorLink.Id != filterLink.Id)
                continue;

            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(initiator);
            while (anchored.MoveNext(out var water))
            {
                if (!TryComp(water, out PurifiableWaterComponent? purifiable) ||
                    !TryComp(water, out WaterLinkComponent? waterLink) ||
                    waterLink.Id != filterLink.Id)
                {
                    continue;
                }

                StartPurification((water, purifiable));
            }
        }
    }

    private void ScheduleActivation(Entity<PurifiableWaterComponent> water, TimeSpan at, Vector2 incomingDirection)
    {
        if (water.Comp.State != PurifiableWaterState.Toxic)
            return;

        if (TryComp(water, out ActiveWaterComponent? active))
        {
            if (active.SpreadAt <= at)
                return;

            active.SpreadAt = at;
            active.IncomingDirection = incomingDirection;
            Dirty(water.Owner, active);
            return;
        }

        active = EnsureComp<ActiveWaterComponent>(water);
        active.SpreadAt = at;
        active.IncomingDirection = incomingDirection;
        Dirty(water.Owner, active);
    }

    private void BeginDispersing(Entity<PurifiableWaterComponent, ActiveWaterComponent> water, TimeSpan time)
    {
        if (water.Comp1.State != PurifiableWaterState.Toxic)
            return;

        water.Comp1.State = PurifiableWaterState.Dispersing;
        water.Comp2.SpreadAt = time + water.Comp1.PurifyDelay;

        // SyncSprite derives animation progress from global real time. Temporarily remove it so
        // this one-shot transition starts from its first frame instead of wrapping mid-transition.
        water.Comp2.RestoreSyncSprite = RemComp<SyncSpriteComponent>(water);
        Dirty(water.Owner, water.Comp1);
        Dirty(water.Owner, water.Comp2);
        UpdateAppearance((water.Owner, water.Comp1));

        if (TryComp(water, out WaterLinkComponent? link) &&
            !string.IsNullOrWhiteSpace(link.Id) &&
            _rmcMap.TryGetTileRefForEnt(water.Owner.ToCoordinates(), out var grid, out var tile))
        {
            foreach (var offset in NeighborOffsets)
            {
                var adjacent = _rmcMap.GetAnchoredEntitiesEnumerator(grid, tile.GridIndices + offset);
                while (adjacent.MoveNext(out var adjacentUid))
                {
                    if (!TryComp(adjacentUid, out PurifiableWaterComponent? adjacentWater) ||
                        adjacentWater.State != PurifiableWaterState.Toxic ||
                        !TryComp(adjacentUid, out WaterLinkComponent? adjacentLink) ||
                        adjacentLink.Id != link.Id)
                    {
                        continue;
                    }

                    var delay = water.Comp1.Delay;
                    if (offset.X != 0 && offset.Y != 0)
                        delay *= Math.Sqrt(2);

                    _pendingActivations.Add(new WaterActivation(
                        (adjacentUid, adjacentWater),
                        time + delay,
                        new Vector2(offset.X, offset.Y)));
                }
            }
        }

        ApplyDispersalEffects(water.Owner, water.Comp1, water.Comp2.IncomingDirection);
    }

    private void ApplyDispersalEffects(EntityUid water, PurifiableWaterComponent purifiable, Vector2 incomingDirection)
    {
        if (_random.Prob(purifiable.SloshChance))
            _audio.PlayPvs(purifiable.SloshSound, water);

        _weedKiller.RemoveMarkedAt(water);

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(water);
        while (anchored.MoveNext(out var anchoredUid))
        {
            if (HasComp<TileFireComponent>(anchoredUid))
                QueueDel(anchoredUid);
        }

        EnsureComp<BlockWeedsComponent>(water);
        EnsureComp<BlockXenoConstructionComponent>(water);

        if (incomingDirection == Vector2.Zero)
            return;

        _items.Clear();
        _entityLookup.GetEntitiesInRange(water.ToCoordinates(), 0.49f, _items, LookupFlags.Uncontained);
        foreach (var item in _items)
        {
            if (item.Owner == water ||
                Transform(item).Anchored ||
                !_random.Prob(purifiable.ThrowChance))
            {
                continue;
            }

            _throwing.TryThrow(
                item,
                Vector2.Normalize(incomingDirection),
                baseThrowSpeed: 5,
                recoil: false,
                compensateFriction: true,
                playSound: false);
        }
    }

    private void FinishPurifying(Entity<PurifiableWaterComponent, ActiveWaterComponent> water)
    {
        water.Comp1.State = PurifiableWaterState.Purified;
        Dirty(water.Owner, water.Comp1);
        UpdateAppearance((water.Owner, water.Comp1));

        if (water.Comp2.RestoreSyncSprite)
            EnsureComp<SyncSpriteComponent>(water);

        RemCompDeferred<DamageOverTimeComponent>(water);
        RemCompDeferred<ActiveWaterComponent>(water);
    }

    private void UpdateFilters(TimeSpan time)
    {
        var filters = EntityQueryEnumerator<WaterFilterComponent>();
        while (filters.MoveNext(out var uid, out var filter))
        {
            if (!filter.Active)
                continue;

            if (!filter.Triggered && time >= filter.TriggerAt)
            {
                filter.Triggered = true;
                Dirty(uid, filter);
                TriggerFilter((uid, filter));
            }

            if (time < filter.ResetAt)
                continue;

            filter.Active = false;
            filter.Triggered = false;
            Dirty(uid, filter);
            _power.SetReceiverMode(uid, RMCPowerMode.Idle);
            UpdateFilterAppearance((uid, filter));
        }
    }

    private void UpdateWater(TimeSpan time)
    {
        _pendingActivations.Clear();

        var query = EntityQueryEnumerator<ActiveWaterComponent, PurifiableWaterComponent>();
        while (query.MoveNext(out var uid, out var active, out var purifiable))
        {
            if (time < active.SpreadAt)
                continue;

            switch (purifiable.State)
            {
                case PurifiableWaterState.Toxic:
                    BeginDispersing((uid, purifiable, active), time);
                    break;
                case PurifiableWaterState.Dispersing:
                    FinishPurifying((uid, purifiable, active));
                    break;
                case PurifiableWaterState.Purified:
                    UpdateAppearance((uid, purifiable));
                    RemCompDeferred<ActiveWaterComponent>(uid);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        foreach (var activation in _pendingActivations)
        {
            ScheduleActivation(activation.Water, activation.At, activation.IncomingDirection);
        }
    }

    private void UpdateToxicVehicles(TimeSpan time)
    {
        var toxicWater = EntityQueryEnumerator<ToxicWaterComponent>();
        while (toxicWater.MoveNext(out var uid, out var toxic))
        {
            if (time < toxic.NextVehicleDamageAt)
                continue;

            toxic.NextVehicleDamageAt = time + toxic.VehicleDamageEvery;
            Dirty(uid, toxic);

            if (!IsHazardous(uid))
                continue;

            var playedSound = false;
            foreach (var vehicle in _physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Vehicle))
            {
                if (!HasComp<VehicleWeakComponent>(vehicle) || !IsActiveWater(uid, vehicle))
                    continue;

                _vehicleWheels.DamageWheels(vehicle, toxic.VehicleDamage);
                playedSound = true;
            }

            if (playedSound)
                _audio.PlayPvs(ToxicWaterSound, uid);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        UpdateFilters(time);
        UpdateWater(time);
        UpdateToxicVehicles(time);
    }

    private readonly record struct WaterActivation(
        Entity<PurifiableWaterComponent> Water,
        TimeSpan At,
        Vector2 IncomingDirection);
}
