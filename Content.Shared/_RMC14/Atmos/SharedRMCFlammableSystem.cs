using System.Linq;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Alert;
using Content.Shared.Atmos;
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
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

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

        SubscribeLocalEvent<RMCIgniteOnCollideComponent, StartCollideEvent>(OnIgniteCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, DamageCollideEvent>(OnIgniteDamageCollide);

        SubscribeLocalEvent<SteppingOnFireComponent, CMGetArmorEvent>(OnSteppingOnFireGetArmor);
        SubscribeLocalEvent<SteppingOnFireComponent, ComponentRemove>(OnSteppingOnFireRemoved);

        SubscribeLocalEvent<CanBeFirePattedComponent, InteractHandEvent>(OnCanBeFirePattedInteractHand, before: [typeof(InteractionPopupSystem)]);

        SubscribeLocalEvent<FlammableComponent, IgnitedEvent>(OnFlammableIgnite);
        SubscribeLocalEvent<FlammableComponent, RMCExtinguishedEvent>(OnFlammableExtinguished);

        SubscribeLocalEvent<PlasmaFrenzyComponent, IgnitedEvent>(OnPlasmaFrenzyIgnite);
    }

    private void OnIgniteOnProjectileHit(Entity<IgniteOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!CanBeIgnited(args.Target, ent))
            return;

        Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.Duration, false);
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

    private void OnIgniteCollide(Entity<RMCIgniteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        TryIgnite(ent, args.OtherEntity, false);
    }

    private void OnIgniteDamageCollide(Entity<RMCIgniteOnCollideComponent> ent, ref DamageCollideEvent args)
    {
        if (!CanBeIgnited(args.Target, ent))
            return;

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

        Pat(ent.Owner, patter.Stacks);

        _audio.PlayPredicted(patter.Sound, user, user);
        _popup.PopupClient($"You try to put out the fire on {Name(ent)}!", ent, user, PopupType.SmallCaution);
        _popup.PopupEntity($"{Name(user)} tries to put out the fire on you!", ent, ent, PopupType.SmallCaution);

        var others = Filter.PvsExcept(ent).RemoveWhereAttachedEntity(e => e == user || e == ent.Owner);
        _popup.PopupEntity($"{Name(user)} tries to put out the fire on {Name(ent)}!", ent, others, true);

    }

    private void OnFlammableIgnite(Entity<FlammableComponent> ent, ref IgnitedEvent args)
    {
        EnsureComp<OnFireComponent>(ent);
    }

    private void OnFlammableExtinguished(Entity<FlammableComponent> ent, ref RMCExtinguishedEvent args)
    {
        RemCompDeferred<OnFireComponent>(ent);
        RemCompDeferred<RMCFireBypassActiveComponent>(ent);
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

    public virtual void Pat(Entity<FlammableComponent?> flammable, int stacks)
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

    private void SpawnFires(EntProtoId spawn, EntityCoordinates coordinates, int range, EntityUid chain, int? intensity, int? duration, HashSet<EntityCoordinates>? spawned = null)
    {
        if (_net.IsClient)
            return;

        spawned ??= new HashSet<EntityCoordinates>();
        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var target = coordinates.Offset(cardinal);
            if (!spawned.Add(target))
                continue;

            var nextRange = SpawnFire(target, spawn, chain, range, intensity, duration, out var cont);
            if (nextRange == 0 || cont)
                continue;

            Timer.Spawn(TimeSpan.FromMilliseconds(50),
                () =>
                {
                    try
                    {
                        SpawnFires(spawn, target, nextRange, chain, intensity, duration, spawned);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error occurred spawning fires:\n{e}");
                    }
                });
        }
    }

    public void SpawnFireDiamond(EntProtoId spawn, EntityCoordinates center, int range, int? intensity = null, int? duration = null)
    {
        var chain = _onCollide.SpawnChain();
        SpawnFires(spawn, center, range, chain, intensity, duration);
    }

    public int SpawnFire(EntityCoordinates target, EntProtoId spawn, EntityUid chain, int range, int? intensity, int? duration, out bool cont)
    {
        cont = false;
        if (!_rmcMap.TryGetTileDef(target, out var tile) ||
            tile.ID == ContentTileDefinition.SpaceID)
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

    /// <summary>
    ///     Spawns fire in a cone shape in the direction the entity is facing.
    /// </summary>
    private void SpawnFireCone(Entity<DirectionalTileFireOnTriggerComponent> ent, EntityCoordinates center, int? intensity = null, int? duration = null)
    {
        var chain = _onCollide.SpawnChain();

        if (_net.IsClient)
            return;

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
            //Logic to get the targets when an entity is facing a cardinal direction
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
        if (!_rmcMap.TryGetTileDef(target, out var tile) ||
            tile.ID == ContentTileDefinition.SpaceID)
        {
            return false;
        }

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
            if (!_prototype.TryIndexReagent(solutionReagent.Reagent.Prototype, out ReagentPrototype? reagent))
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

    private void OnPlasmaFrenzyIgnite(Entity<PlasmaFrenzyComponent> ent, ref IgnitedEvent args)
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
        EnsureComp<SteppingOnFireComponent>(other);
        var flammableEnt = new Entity<FlammableComponent?>(other, null);
        if (!Resolve(flammableEnt, ref flammableEnt.Comp, false))
            return;

        var wasOnFire = IsOnFire(flammableEnt);
        if (checkIgnited && wasOnFire)
            return;

        if (!CanBeIgnited(other, ent))
            return;

        // Check RMCImmuneToFireTileDamageComponent for ignition immunity
        if (TryComp<RMCImmuneToFireTileDamageComponent>(other, out var fireImmunity) && fireImmunity.BypassWhitelist != null)
        {
            // Check if this fire is on the bypass whitelist
            if (!_entityWhitelist.IsWhitelistPass(fireImmunity.BypassWhitelist, ent))
            {
                // Fire is not on bypass whitelist, cannot ignite
                return;
            }
        }

        if (!Ignite(flammableEnt, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks))
            return;

        // If this fire can bypass immunity, mark the target as having bypass-active fire
        if (!CanFireBypassImmunity(ent, other))
        {
            // If the fire can't bypass immunity, ensure the bypass component is removed
            RemCompDeferred<RMCFireBypassActiveComponent>(other);
        }
        else
        {
            // If the fire can bypass immunity, add the bypass component
            EnsureComp<RMCFireBypassActiveComponent>(other);
        }

        if (!wasOnFire && IsOnFire(flammableEnt) && CanFireBypassImmunity(ent, other))
            _damageable.TryChangeDamage(flammableEnt, flammableEnt.Comp.Damage * ent.Comp.Intensity, true);
    }

    private void ApplyTileEffect(Entity<SteppingOnFireComponent> ent, RMCIgniteOnCollideComponent ignite, EntityUid fireEntity)
    {
        if (ignite.TileDamage is not { } tile)
            return;

        var stepping = ent.Comp;
        var uid = ent.Owner;

        if (ignite.ArmorMultiplier < stepping.ArmorMultiplier &&
            _entityWhitelist.IsWhitelistPassOrNull(ignite.ArmorWhitelist, uid))
        {
            stepping.ArmorMultiplier = ignite.ArmorMultiplier;
            if (TryComp<RMCFireArmorDebuffModifierComponent>(uid, out var mod))
                stepping.ArmorMultiplier *= mod.DebuffModifier;
            _armor.UpdateArmorValue((uid, null));
        }

        var coords = _transform.GetMoverCoordinates(uid);
        if (stepping.LastPosition is { } last &&
            last.TryDistance(EntityManager, _transform, coords, out var distance))
        {
            stepping.Distance += distance;
            if (stepping.Distance >= 1)
            {
                stepping.Distance = 0;
                if (CanFireBypassImmunity(fireEntity, uid))
                    _damageable.TryChangeDamage(uid, tile * ignite.Intensity);
            }
        }

        // Check for ignition immunity before igniting
        bool canIgnite = CanBeIgnited(uid, fireEntity);

        if (canIgnite)
            Ignite(uid, ignite.Intensity, ignite.Duration, ignite.MaxStacks);

        // If this fire can bypass immunity, mark the target as having bypass-active fire
        if (canIgnite)
        {
            if (!CanFireBypassImmunity(fireEntity, uid))
            {
                // If the fire can't bypass immunity, ensure the bypass component is removed
                RemCompDeferred<RMCFireBypassActiveComponent>(uid);
            }
            else
            {
                // If the fire can bypass immunity, add the bypass component
                EnsureComp<RMCFireBypassActiveComponent>(uid);
            }
        }

        stepping.LastPosition = coords;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var applyQuery = EntityQueryEnumerator<RMCIgniteOnCollideComponent>();
        while (applyQuery.MoveNext(out var uid, out var apply))
        {
            foreach (var contact in _physics.GetEntitiesIntersectingBody(uid, (int)apply.Collision))
            {
                TryIgnite((uid, apply), contact, true);
            }

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

            var intersecting = _physics.GetEntitiesIntersectingBody(uid, (int)extinguish.Collision);
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

        var tileExtinguishQuery = EntityQueryEnumerator<SprayExtinguishTileFireComponent>();
        while (tileExtinguishQuery.MoveNext(out var uid, out var extinguishTile))
        {
            if (extinguishTile.Extinguished)
                continue;

            extinguishTile.Extinguished = true;
            Dirty(uid, extinguishTile);

            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(uid);

            while (anchored.MoveNext(out var anchorUid))
            {
                if (!_tileFireQuery.TryComp(anchorUid, out var tileFire))
                    continue;

                tileFire.Duration -= extinguishTile.ExtinguishAmount * tileFire.SprayExtinguishMultiplier;
                Dirty(anchorUid, tileFire);
            }
        }

        var steppingQuery = EntityQueryEnumerator<SteppingOnFireComponent>();
        while (steppingQuery.MoveNext(out var uid, out var stepping))
        {
            stepping.ArmorMultiplier = 1;
            Dirty(uid, stepping);

            var isStepping = false;
            foreach (var contact in _physics.GetContactingEntities(uid, approximate: true))
            {
                if (!_igniteOnCollideQuery.TryComp(contact, out var ignite))
                    continue;

                ApplyTileEffect((uid, stepping), ignite, contact);
                isStepping = true;
                break;
            }

            // Entities with a static body type do not have any contacts with tile fires, so we do another standing on fire check.
            if (!isStepping)
            {
                var nearbyEntities = _entityLookup.GetEntitiesInRange<RMCIgniteOnCollideComponent>(Transform(uid).Coordinates, 0.35f);
                if (nearbyEntities.Count != 0)
                {
                    var nearbyEntity = nearbyEntities.First();
                    ApplyTileEffect((uid, stepping), nearbyEntity.Comp, nearbyEntity.Owner);
                    isStepping = true;
                }
            }

            if (!isStepping)
                RemCompDeferred<SteppingOnFireComponent>(uid);
        }
    }

    /// <summary>
    /// Checks if a fire entity can deal damage to an entity that has fire immunity.
    /// </summary>
    /// <param name="fireEntity">The fire entity (like a tile fire) that might deal damage</param>
    /// <param name="targetEntity">The entity that might have fire immunity</param>
    /// <returns>True if damage should be dealt, false if it should be blocked by immunity</returns>
    private bool CanFireBypassImmunity(EntityUid fireEntity, EntityUid targetEntity)
    {
        // If the target doesn't have fire immunity, fire always works
        if (!TryComp<RMCImmuneToFireTileDamageComponent>(targetEntity, out var immunity))
            return true;

        // If the fire has a bypass component, it can always bypass immunity
        if (HasComp<RMCFireImmunityBypassComponent>(fireEntity))
            return true;

        // If immunity has a bypass whitelist, check if this fire is on it
        if (immunity.BypassWhitelist != null)
        {
            return _entityWhitelist.IsWhitelistPass(immunity.BypassWhitelist, fireEntity);
        }

        // No bypass mechanisms apply, immunity blocks the damage
        return false;
    }

    /// <summary>
    /// Checks if an entity can be ignited by a specific fire source, considering immunity components.
    /// </summary>
    /// <param name="target">The entity that might be ignited</param>
    /// <param name="fireSource">The fire source attempting to ignite the target</param>
    /// <returns>True if the target can be ignited, false if immunity blocks it</returns>
    private bool CanBeIgnited(EntityUid target, EntityUid fireSource)
    {
        // Check for complete ignition immunity first
        if (TryComp<RMCImmuneToIgnitionComponent>(target, out var ignitionImmunity))
        {
            // If entity has complete ignition immunity, check if this fire can bypass it
            if (ignitionImmunity.BypassWhitelist == null)
            {
                // Complete immunity - no fires can ignite this entity
                return false;
            }
            else
            {
                // Check if this fire is on the bypass whitelist
                if (!_entityWhitelist.IsWhitelistPass(ignitionImmunity.BypassWhitelist, fireSource))
                {
                    // Fire is not on bypass whitelist, cannot ignite
                    return false;
                }
            }
        }

        return true;
    }
}
