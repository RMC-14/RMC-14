using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
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
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;

    private static readonly ProtoId<AlertPrototype> FireAlert = "Fire";
    private static readonly ProtoId<ReagentPrototype> WaterReagent = "Water";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private EntityQuery<BlockTileFireComponent> _blockTileFireQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<FlammableComponent> _flammableQuery;
    private EntityQuery<RMCIgniteOnCollideComponent> _igniteOnCollideQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<TileFireComponent> _tileFireQuery;

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

        SubscribeLocalEvent<RMCIgniteOnCollideComponent, StartCollideEvent>(OnIgniteCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, DamageCollideEvent>(OnIgniteDamageCollide);

        SubscribeLocalEvent<SteppingOnFireComponent, CMGetArmorEvent>(OnSteppingOnFireGetArmor);
        SubscribeLocalEvent<SteppingOnFireComponent, ComponentRemove>(OnSteppingOnFireRemoved);

        SubscribeLocalEvent<CanBeFirePattedComponent, InteractHandEvent>(OnCanBeFirePattedInteractHand, before: [typeof(InteractionPopupSystem)]);

        SubscribeLocalEvent<FlammableComponent, RMCIgniteEvent>(OnFlammableIgnite);
        SubscribeLocalEvent<FlammableComponent, RMCExtinguishedEvent>(OnFlammableExtinguished);
    }

    private void OnIgniteOnProjectileHit(Entity<IgniteOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        Ignite(args.Target, ent.Comp.Stacks, ent.Comp.Intensity, ent.Comp.Duration, false);
    }

    private void OnTileFireMapInit(Entity<TileFireComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpawnedAt = _timing.CurTime;
        Dirty(ent);
    }

    private void OnTileFireVaporHit(Entity<TileFireComponent> ent, ref VaporHitEvent args)
    {
        if (_net.IsClient)
            return;

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

        if (!water)
            return;

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
        if (time < patter.Last + patter.Cooldown)
            return;

        patter.Last = time;
        Dirty(user, patter);

        ent.Comp.Duration -= patter.RemoveDuration * ent.Comp.PatExtinguishMultiplier;
        Dirty(ent);

        _rmcMelee.DoLunge(user, ent);
        _audio.PlayPredicted(patter.Sound, user, user, AudioParams.Default.WithVolume(-8).WithVariation(0.05f));
    }

    private void OnTileFirePreventCollide(Entity<TileFireComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

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

        if (_net.IsClient)
            return;

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

    private void OnTileFireOnTriggerExplosive(Entity<TileFireOnTriggerComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        var coords = _transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager, _map);
        SpawnFireDiamond(ent.Comp.Spawn, coords, ent.Comp.Range, ent.Comp.Intensity, ent.Comp.Duration);
    }

    private void OnIgniteCollide(Entity<RMCIgniteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        var flammableEnt = new Entity<FlammableComponent?>(args.OtherEntity, null);
        if (!Resolve(flammableEnt, ref flammableEnt.Comp, false))
            return;

        var wasOnFire = IsOnFire(flammableEnt);
        if (!Ignite(flammableEnt, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks))
            return;

        EnsureComp<SteppingOnFireComponent>(args.OtherEntity);

        if (!wasOnFire && IsOnFire(flammableEnt))
            _damageable.TryChangeDamage(flammableEnt, flammableEnt.Comp.Damage * ent.Comp.Intensity, true);
    }

    private void OnIgniteDamageCollide(Entity<RMCIgniteOnCollideComponent> ent, ref DamageCollideEvent args)
    {
        Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks);
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
        if (args.Target != ent.Owner ||
            user == args.Target ||
            !TryComp(user, out FirePatterComponent? patter) ||
            _entityWhitelist.IsBlacklistPass(patter.Blacklist, ent) ||
            !TryComp(ent, out FlammableComponent? flammable) ||
            !flammable.OnFire)
        {
            return;
        }

        args.Handled = true;
        var time = _timing.CurTime;
        if (time < patter.LastPat + patter.Cooldown)
            return;

        patter.LastPat = time;
        Dirty(user, patter);

        Pat(ent.Owner);

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

    public virtual void Extinguish(Entity<FlammableComponent?> flammable)
    {
    }

    public virtual void Pat(Entity<FlammableComponent?> flammable)
    {
    }

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

    private void SpawnFires(EntProtoId spawn, EntityCoordinates coordinates, int range, EntityUid chain, int? intensity, int? duration)
    {
        if (_net.IsClient)
            return;

        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var target = coordinates.Offset(cardinal);
            if (!_rmcMap.TryGetTileDef(target, out var tile) ||
                tile.ID == ContentTileDefinition.SpaceID)
            {
                continue;
            }

            if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(target, out var oldTileFire))
            {
                if (spawn == oldTileFire.Comp.Id)
                    continue;

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
            if (nextRange == 0)
                continue;

            Timer.Spawn(TimeSpan.FromMilliseconds(50),
                () =>
                {
                    SpawnFires(spawn, target, nextRange, chain, intensity, duration);
                });
        }
    }

    public void SpawnFireDiamond(EntProtoId spawn, EntityCoordinates center, int range, int? intensity = null, int? duration = null)
    {
        var chain = _onCollide.SpawnChain();
        SpawnFires(spawn, center, range, chain, intensity, duration);
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

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var applyQuery = EntityQueryEnumerator<RMCIgniteOnCollideComponent>();
        while (applyQuery.MoveNext(out var uid, out var apply))
        {
            if (apply.InitDamaged)
                continue;

            apply.InitDamaged = true;
            Dirty(uid, apply);

            RemCompDeferred<DamageOnCollideComponent>(uid);
        }

        var time = _timing.CurTime;
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

            if (time < fire.SpawnedAt + fire.BigFireDuration)
                _appearance.SetData(uid, TileFireLayers.Base, TileFireVisuals.Four);
            else if (timeLeft < TimeSpan.FromSeconds(9))
                _appearance.SetData(uid, TileFireLayers.Base, TileFireVisuals.One);
            else if (timeLeft < TimeSpan.FromSeconds(25))
                _appearance.SetData(uid, TileFireLayers.Base, TileFireVisuals.Two);
            else
                _appearance.SetData(uid, TileFireLayers.Base, TileFireVisuals.Three);
        }

        var extinguishQuery = EntityQueryEnumerator<ExtinguishFireComponent>();
        while (extinguishQuery.MoveNext(out var uid, out var extinguish))
        {
            if (extinguish.Extinguished)
                continue;

            extinguish.Extinguished = true;
            Dirty(uid, extinguish);

            var intersecting = _physics.GetEntitiesIntersectingBody(uid, (int) extinguish.Collision);
            foreach (var entIntersecting in intersecting)
            {
                if (!_flammableQuery.TryComp(entIntersecting, out var flammable))
                    continue;

                var ev = new ExtinguishFireAttemptEvent(uid, entIntersecting);
                RaiseLocalEvent(uid, ref ev);

                if (!ev.Cancelled)
                    Extinguish((entIntersecting, flammable));
            }
        }

        var steppingQuery = EntityQueryEnumerator<SteppingOnFireComponent>();
        while (steppingQuery.MoveNext(out var uid, out var stepping))
        {
            stepping.ArmorMultiplier = 1;
            Dirty(uid, stepping);

            var isStepping = false;
            foreach (var contact in _physics.GetContactingEntities(uid))
            {
                if (!_igniteOnCollideQuery.TryComp(contact, out var ignite) ||
                    ignite.TileDamage is not { } tile)
                {
                    continue;
                }

                if (ignite.ArmorMultiplier < stepping.ArmorMultiplier &&
                    _entityWhitelist.IsWhitelistPassOrNull(ignite.ArmorWhitelist, uid))
                {
                    stepping.ArmorMultiplier = ignite.ArmorMultiplier;
                    _armor.UpdateArmorValue((uid, null));
                }

                isStepping = true;
                var coords = _transform.GetMoverCoordinates(uid);
                if (stepping.LastPosition is { } last &&
                    last.TryDistance(EntityManager, _transform, coords, out var distance))
                {
                    stepping.Distance += distance;
                    if (stepping.Distance >= 1)
                    {
                        stepping.Distance = 0;
                        _damageable.TryChangeDamage(uid, tile * ignite.Intensity);
                    }
                }

                Ignite(uid, ignite.Intensity, ignite.Duration, ignite.MaxStacks);

                stepping.LastPosition = coords;
                break;
            }

            if (!isStepping)
                RemCompDeferred<SteppingOnFireComponent>(uid);
        }
    }
}
