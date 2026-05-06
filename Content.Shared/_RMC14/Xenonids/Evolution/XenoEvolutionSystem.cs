using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cloning;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Evolution;

public sealed class XenoEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _xenoHive = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    private TimeSpan _evolutionPointsRequireOvipositorAfter;
    private TimeSpan _evolutionAccumulatePointsBefore;
    private TimeSpan _evolveSameCasteCooldown;
    private TimeSpan _earlyEvoBoostBefore;

    private readonly HashSet<EntityUid> _climbable = new();
    private readonly HashSet<EntityUid> _doors = new();
    private readonly HashSet<EntityUid> _intersecting = new();

    private EntityQuery<MobStateComponent> _mobStateQuery;

    public readonly ProtoId<CloningSettingsPrototype> CloningSettingsId = "XenoClone";

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeNetworkEvent<XenoChangingCasteEvent>(OnXenoChangingCaste);

        SubscribeLocalEvent<FixturesComponent, XenoChangingPrototypeEvent>(OnFixturesXenoChangingPrototype);

        SubscribeLocalEvent<XenoDevolveComponent, XenoOpenDevolveActionEvent>(OnXenoOpenDevolveAction);

        SubscribeLocalEvent<XenoEvolutionComponent, MapInitEvent>(OnXenoEvolveMapInit);
        SubscribeLocalEvent<XenoEvolutionComponent, ComponentShutdown>(OnXenoEvolveShutdown);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoOpenEvolutionsActionEvent>(OnXenoEvolveAction);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoChangingPrototypeEvent>(OnXenoChangingPrototype);
        SubscribeLocalEvent<XenoEvolutionComponent, AfterXenoChangedCasteEvent>(OnAfterXenoChangedCaste);

        SubscribeLocalEvent<XenoNewlyEvolvedComponent, PreventCollideEvent>(OnNewlyEvolvedPreventCollide);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, AfterXenoChangedCasteEvent>(OnGranterEvolved);

        SubscribeLocalEvent<XenoOvipositorChangedEvent>(OnOvipositorChanged);

        Subs.BuiEvents<XenoEvolutionComponent>(XenoEvolutionUIKey.Key,
            subs =>
            {
                subs.Event<XenoEvolveBuiMsg>(OnXenoEvolveBui);
                subs.Event<XenoStrainBuiMsg>(OnXenoStrainBui);
            });

        Subs.BuiEvents<XenoDevolveComponent>(XenoDevolveUIKey.Key,
            subs =>
            {
                subs.Event<XenoDevolveBuiMsg>(OnXenoDevolveBui);
            });

        Subs.CVar(_config, RMCCVars.RMCEvolutionPointsRequireOvipositorMinutes, v => _evolutionPointsRequireOvipositorAfter = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCEvolutionPointsAccumulateBeforeMinutes, v => _evolutionAccumulatePointsBefore = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCXenoEvolveSameCasteCooldownSeconds, v => _evolveSameCasteCooldown = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCXenoEarlyEvoPointBoostBeforeMinutes, v => _earlyEvoBoostBefore = TimeSpan.FromMinutes(v), true);
    }

    private void OnXenoOpenDevolveAction(Entity<XenoDevolveComponent> xeno, ref XenoOpenDevolveActionEvent args)
    {
        if (args.Handled)
            return;

        if (!DamagedCheckPopup(xeno))
            return;

        args.Handled = true;
        _ui.OpenUi(xeno.Owner, XenoDevolveUIKey.Key, xeno);
    }

    private void OnXenoEvolveMapInit(Entity<XenoEvolutionComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnXenoEvolveShutdown(Entity<XenoEvolutionComponent> ent, ref ComponentShutdown ev)
    {
        _action.RemoveAction(ent.Comp.Action);
    }

    private void OnXenoEvolveAction(Entity<XenoEvolutionComponent> xeno, ref XenoOpenEvolutionsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _ui.OpenUi(xeno.Owner, XenoEvolutionUIKey.Key, xeno);

        var state = new XenoEvolveBuiState(LackingOvipositor());
        _ui.SetUiState(xeno.Owner, XenoEvolutionUIKey.Key, state);
    }

    private void OnXenoEvolveBui(Entity<XenoEvolutionComponent> xeno, ref XenoEvolveBuiMsg args)
    {
        var actor = args.Actor;
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, actor);

        if (_net.IsClient)
            return;

        if (!CanEvolvePopup(xeno, args.Choice))
        {
            Log.Warning($"{ToPrettyString(actor)} sent an invalid evolution choice: {args.Choice}.");
            return;
        }

        if (!DamagedCheckPopup(xeno, false))
            return;

        var time = _timing.CurTime;
        if (_prototypes.TryIndex(args.Choice, out var choice) &&
            choice.HasComponent<XenoEvolutionGranterComponent>(_compFactory) &&
            _xenoHive.GetHive(xeno.Owner) is { } hive &&
            hive.Comp.LastQueenDeath is { } lastQueenDeath &&
            time < lastQueenDeath + hive.Comp.NewQueenCooldown)
        {
            var left = lastQueenDeath + hive.Comp.NewQueenCooldown - time;
            var msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-recent-queen-death-minutes",
                ("minutes", left.Minutes),
                ("seconds", left.Seconds));
            if (left.Minutes == 0)
            {
                msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-recent-queen-death-seconds",
                    ("seconds", left.Seconds));
            }

            _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);
            return;
        }

        var ev = new XenoEvolutionDoAfterEvent(args.Choice);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.EvolutionDelay, ev, xeno)
        {
            BreakOnRest = false,
        };

        if (xeno.Comp.EvolutionDelay > TimeSpan.Zero)
            _popup.PopupClient(Loc.GetString("cm-xeno-evolution-start"), xeno, xeno);

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _jitter.DoJitter(xeno, xeno.Comp.EvolutionDelay, true, 80, 8, true);

            var popupOthers = Loc.GetString("rmc-xeno-evolution-start-others", ("xeno", xeno));
            _popup.PopupEntity(popupOthers, xeno, Filter.PvsExcept(xeno), true, PopupType.Medium);

            var popupSelf = Loc.GetString("rmc-xeno-evolution-start-self");
            _popup.PopupEntity(popupSelf, xeno, xeno, PopupType.Medium);
        }
    }

    private void OnXenoStrainBui(Entity<XenoEvolutionComponent> xeno, ref XenoStrainBuiMsg args)
    {
        var actor = args.Actor;
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, actor);

        if (_net.IsClient)
            return;

        if (!xeno.Comp.Strains.Contains(args.Choice))
        {
            Log.Warning($"{ToPrettyString(actor)} sent an invalid strain choice: {args.Choice}.");
            return;
        }

        if (!ContainedCheckPopup(xeno))
            return;

        if (!DamagedCheckPopup(xeno, false))
            return;

        var ev = new AfterXenoChangedCasteEvent(xeno, xeno.Comp.Points, false);
        ChangeCaste(xeno, args.Choice);
        RaiseLocalEvent(xeno, ref ev, true);

        _adminLog.Add(LogType.RMCEvolve, $"Xenonid {ToPrettyString(xeno)} chose strain {args.Choice.ToString()}");
    }

    private void OnXenoDevolveBui(Entity<XenoDevolveComponent> xeno, ref XenoDevolveBuiMsg args)
    {
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, xeno);
        TryDevolve(xeno, args.Choice);
    }

    private void OnXenoEvolveDoAfter(Entity<XenoEvolutionComponent> xeno, ref XenoEvolutionDoAfterEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            args.Cancelled ||
            !_mind.TryGetMind(xeno, out _, out _) ||
            !CanEvolvePopup(xeno, args.Choice))
        {
            return;
        }

        var newPointTotal = FixedPoint2.Max(0, xeno.Comp.Points - xeno.Comp.Max);
        var ev = new AfterXenoChangedCasteEvent(xeno, newPointTotal, false);
        ChangeCaste(xeno, args.Choice);
        RaiseLocalEvent(xeno, ref ev, true);

        _adminLog.Add(LogType.RMCEvolve, $"Xenonid {ToPrettyString(xeno)} evolved into proto {args.Choice.ToString()}");

        _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-end"), xeno, xeno);
    }

    private void OnXenoChangingPrototype(Entity<XenoEvolutionComponent> xeno, ref XenoChangingPrototypeEvent args)
    {
        var compName = EntityManager.ComponentFactory.GetComponentName<XenoEvolutionComponent>();
        if (args.NewComponents.TryGetComponent(compName, out var c) && c is XenoEvolutionComponent { } newComponent)
        {
            // transfer points over.
            // points are deducted when handling AfterXenoChangedCasteEvent, as only the
            // caller of ChangeCaste knows how many points to deduct.
            newComponent.Points = xeno.Comp.Points;
            newComponent.LastPointsAt = xeno.Comp.LastPointsAt;
        }
    }

    private void OnFixturesXenoChangingPrototype(Entity<FixturesComponent> xeno, ref XenoChangingPrototypeEvent args)
    {
        var compName = EntityManager.ComponentFactory.GetComponentName<FixturesComponent>();
        if (args.NewComponents.TryGetComponent(compName, out var c) && c is FixturesComponent { } newComponent)
        {
            // TODO RMC14 I'm unsure of why the fixtures aren't properly removed when the FixturesComponent is removed,
            // but it causes some issues in the physics system. We remove them here.
            foreach (var id in xeno.Comp.Fixtures.Keys)
            {
                _fixtures.DestroyFixture(xeno, id);
            }
        }
    }

    private void OnAfterXenoChangedCaste(Entity<XenoEvolutionComponent> xeno, ref AfterXenoChangedCasteEvent args)
    {
        xeno.Comp.Points = FixedPoint2.Max(0, args.NewPointTotal);
        if (!args.Devolved)
        {
            _jitter.DoJitter(xeno, xeno.Comp.EvolutionJitterDuration, true, 80, 8, true);
        }
    }

    private void OnNewlyEvolvedPreventCollide(Entity<XenoNewlyEvolvedComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.StopCollide.Contains(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnGranterEvolved(Entity<XenoEvolutionGranterComponent> ent, ref AfterXenoChangedCasteEvent args)
    {
        _xenoAnnounce.AnnounceSameHive(ent.Owner, Loc.GetString("rmc-new-queen"));
    }

    private void OnOvipositorChanged(ref XenoOvipositorChangedEvent ev)
    {
        if (_net.IsClient)
            return;

        var xenos = EntityQueryEnumerator<ActorComponent, XenoEvolutionComponent>();
        var state = new XenoEvolveBuiState(LackingOvipositor());
        while (xenos.MoveNext(out var uid, out _, out _))
        {
            _ui.SetUiState(uid, XenoEvolutionUIKey.Key, state);
        }
    }

    private bool ContainedCheckPopup(EntityUid xeno, bool doPopup = true)
    {
        if (!_container.IsEntityInContainer(xeno))
            return true;

        if (doPopup)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-failed-bad-location"), xeno, xeno, PopupType.MediumCaution);

        return false;
    }

    private bool DamagedCheckPopup(EntityUid xeno, bool predicted = true, bool doPopup = true)
    {
        if (!TryComp(xeno, out DamageableComponent? damageable) ||
            damageable.TotalDamage <= 1)
            return true;

        if (predicted)
            _popup.PopupClient(Loc.GetString("rmc-xeno-evolution-cant-evolve-damaged"), xeno, xeno, PopupType.MediumCaution);
        else
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-cant-evolve-damaged"), xeno, xeno, PopupType.MediumCaution);

        return false;
    }

    private bool CanEvolvePopup(Entity<XenoEvolutionComponent> xeno, EntProtoId newXeno, bool doPopup = true)
    {
        if (!xeno.Comp.EvolvesTo.Contains(newXeno) && !xeno.Comp.EvolvesToWithoutPoints.Contains(newXeno))
            return false;

        if (!_prototypes.TryIndex(newXeno, out var prototype))
            return true;

        if (!ContainedCheckPopup(xeno, doPopup))
            return false;

        // TODO RMC14 revive jelly when added should not bring back dead queens
        if (prototype.TryGetComponent(out XenoEvolutionCappedComponent? capped, _compFactory) &&
            HasLiving<XenoEvolutionCappedComponent>(capped.Max, e => e.Comp.Id == capped.Id))
        {
            if (doPopup)
                _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-failed-already-have", ("prototype", prototype.Name)), xeno, xeno, PopupType.MediumCaution);

            return false;
        }

        // TODO RMC14 only allow evolving towards Queen if none is alive
        if (!xeno.Comp.CanEvolveWithoutGranter && !HasLiving<XenoEvolutionGranterComponent>(1))
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("cm-xeno-evolution-failed-hive-shaken"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }


        if (TryComp<RestrictEvolveOffWeedsComponent>(xeno.Owner, out var comp))
        {
            var coordinates = _transform.GetMoverCoordinates(xeno).SnapToGrid(EntityManager, _map);
            if (_transform.GetGrid(coordinates) is not { } gridUid ||
                !TryComp(gridUid, out MapGridComponent? grid))
            {
                return false;
            }

            if (!_xenoWeeds.IsOnWeeds((gridUid, grid), coordinates) && comp.RestrictTime > _gameTicker.RoundDuration())
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-evolution-failed-early-weeds"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
                return false;
            }
        }

        prototype.TryGetComponent(out XenoComponent? newXenoComp, _compFactory);
        if (newXenoComp != null &&
            newXenoComp.UnlockAt > _gameTicker.RoundDuration())
        {
            if (doPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString("cm-xeno-evolution-failed-cannot-support"),
                    xeno,
                    xeno,
                    PopupType.MediumCaution
                );
            }

            return false;
        }

        if (newXenoComp != null &&
            !newXenoComp.BypassTierCount &&
            _xenoHive.GetHive(xeno.Owner) is { } oldHive &&
            _xenoHive.TryGetTierLimit((oldHive, oldHive.Comp), newXenoComp.Tier, out var limit))
        {
            var existing = 0;
            var total = Math.Sqrt(oldHive.Comp.BurrowedLarva * oldHive.Comp.BurrowedLarvaSlotFactor);
            total = Math.Min(total, oldHive.Comp.BurrowedLarva);

            var current = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
            var slotCount = oldHive.Comp.FreeSlots.ToDictionary();
            while (current.MoveNext(out var uid, out var existingComp, out var member))
            {
                if (_mobState.IsDead(uid))
                    continue;

                if (member.Hive != oldHive.Owner || !existingComp.CountedInSlots)
                    continue;

                total++;

                if (existingComp.Tier < newXenoComp.Tier)
                    continue;

                if (slotCount.ContainsKey(existingComp.Role.Id) && slotCount[existingComp.Role.Id] > 0)
                    slotCount[existingComp.Role.Id] -= 1;
                else
                    existing++;
            }

            if (total != 0 && existing / (float) total >= limit && (!slotCount.ContainsKey(newXeno) || slotCount[newXeno] <= 0))
            {
                if (doPopup)
                {
                    _popup.PopupEntity(
                        Loc.GetString("cm-xeno-evolution-failed-hive-full", ("tier", newXenoComp.Tier)),
                        xeno,
                        xeno,
                        PopupType.MediumCaution
                    );
                }

                return false;
            }
        }

        if (TryComp(xeno, out XenoRecentlyDevolvedComponent? recently) &&
            recently.Recent.TryGetValue(newXeno, out var at) &&
            at + _evolveSameCasteCooldown > _timing.CurTime)
        {
            var timeRemaining = at + _evolveSameCasteCooldown - _timing.CurTime;
            var msg = Loc.GetString("rmc-xeno-evolution-cant-evolve-caste-cooldown",
                ("minutes", timeRemaining.Minutes),
                ("seconds", timeRemaining.Seconds));

            if (doPopup)
                _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    private bool CanEvolveAny(Entity<XenoEvolutionComponent> xeno)
    {
        if (xeno.Comp.Points >= xeno.Comp.Max && xeno.Comp.EvolvesTo.Count > 0)
            return true;

        foreach (var evolution in xeno.Comp.EvolvesToWithoutPoints)
        {
            if (CanEvolvePopup(xeno, evolution, false))
                return true;
        }

        return false;
    }

    // TODO RMC14 make this a property of the hive component
    // TODO RMC14 per-hive
    public int GetLiving<T>(Predicate<Entity<T>>? predicate = null) where T : IComponent
    {
        var total = 0;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobStateQuery.TryComp(uid, out var mobState) &&
                _mobState.IsDead(uid, mobState))
            {
                continue;
            }

            if (predicate != null && !predicate((uid, comp)))
                continue;

            total++;
        }

        return total;
    }

    // TODO RMC14 make this a property of the hive component
    // TODO RMC14 per-hive
    public bool HasLiving<T>(int count, Predicate<Entity<T>>? predicate = null) where T : IComponent
    {
        if (count <= 0)
            return true;

        var total = 0;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobStateQuery.TryComp(uid, out var mobState) &&
                _mobState.IsDead(uid, mobState))
            {
                continue;
            }

            if (predicate != null && !predicate((uid, comp)))
                continue;

            total++;

            if (total >= count)
                return true;
        }

        return false;
    }

    public FixedPoint2 AddPointsCapped(Entity<XenoEvolutionComponent?> evolution, FixedPoint2 points)
    {
        if (!Resolve(evolution, ref evolution.Comp, false))
            return FixedPoint2.Zero;

        var oldPoints = evolution.Comp.Points;
        evolution.Comp.Points += FixedPoint2.Min(evolution.Comp.Max, points);
        Dirty(evolution);

        return evolution.Comp.Points - oldPoints;
    }

    public void SetPoints(Entity<XenoEvolutionComponent> evolution, FixedPoint2 points)
    {
        evolution.Comp.Points = points;
        Dirty(evolution);
    }

    public bool NeedsOvipositor()
    {
        return _gameTicker.RoundDuration() > _evolutionPointsRequireOvipositorAfter;
    }

    public bool HasOvipositor()
    {
        return HasLiving<XenoEvolutionGranterComponent>(1, e => HasComp<XenoAttachedOvipositorComponent>(e));
    }

    public bool LackingOvipositor()
    {
        return NeedsOvipositor() && !HasOvipositor();
    }

    private void ChangeCaste(EntityUid xeno, EntProtoId protoId)
    {
        // Only server can initiate changing a xeno's caste.
        if (_net.IsClient)
            return;

        RaiseNetworkEvent(new XenoChangingCasteEvent(EntityManager.GetNetEntity(xeno), protoId), Filter.Pvs(xeno));

        var newProto = _prototypes.Index(protoId);

        var recently = EnsureComp<XenoRecentlyDevolvedComponent>(xeno);
        if (Prototype(xeno)?.ID is { } oldId)
            recently.Recent[oldId] = _timing.CurTime;

        ChangeXenoPrototype(xeno, newProto);

        var comp = EnsureComp<XenoNewlyEvolvedComponent>(xeno);
    }

    private void TryDevolve(Entity<XenoDevolveComponent> xeno, EntProtoId to, bool damagedCheck = true)
    {
        if (damagedCheck && !DamagedCheckPopup(xeno))
            return;

        if (Devolve(xeno, to) && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-evolution-devolve", ("xeno", xeno)), xeno, xeno, PopupType.LargeCaution);
    }

    public bool Devolve(Entity<XenoDevolveComponent> xeno, EntProtoId to)
    {
        if (_net.IsClient ||
            !xeno.Comp.DevolvesTo.Contains(to))
        {
            return false;
        }

        TryComp<XenoEvolutionComponent>(xeno, out var evoComp);

        var ev = new AfterXenoChangedCasteEvent(xeno.Owner, evoComp?.Points ?? 0, true);
        ChangeCaste(xeno, to);
        RaiseLocalEvent(xeno, ref ev, true);

        _adminLog.Add(LogType.RMCDevolve, $"Xenonid {ToPrettyString(xeno)} devolved into {to.Id}");

        return true;
    }

    public override void Update(float frameTime)
    {
        var newly = EntityQueryEnumerator<XenoNewlyEvolvedComponent>();
        while (newly.MoveNext(out var uid, out var comp))
        {
            if (comp.TriedClimb)
            {
                _intersecting.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _intersecting);
                for (var i = comp.StopCollide.Count - 1; i >= 0; i--)
                {
                    var colliding = comp.StopCollide[i];
                    if (!_intersecting.Contains(colliding))
                        comp.StopCollide.RemoveAt(i);
                }

                if (comp.StopCollide.Count == 0)
                    RemCompDeferred<XenoNewlyEvolvedComponent>(uid);

                continue;
            }

            comp.TriedClimb = true;
            if (TryComp(uid, out ClimbingComponent? climbing))
            {
                _climbable.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _climbable);

                foreach (var intersecting in _climbable)
                {
                    if (HasComp<ClimbableComponent>(intersecting))
                    {
                        _climb.ForciblySetClimbing(uid, intersecting);
                        Dirty(uid, climbing);
                        break;
                    }
                }
            }
        }

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var roundDuration = _gameTicker.RoundDuration();
        var needsOvipositor = NeedsOvipositor();
        var hasGranter = needsOvipositor
            ? HasOvipositor()
            : HasLiving<XenoEvolutionGranterComponent>(1);
        if (needsOvipositor)
        {
            var granters = EntityQueryEnumerator<XenoEvolutionGranterComponent>();
            while (granters.MoveNext(out var uid, out var granter))
            {
                if (granter.GotOvipositorPopup)
                    continue;

                granter.GotOvipositorPopup = true;
                Dirty(uid, granter);

                _popup.PopupEntity("It is time to settle down and let your children grow.",
                    uid,
                    uid,
                    PopupType.LargeCaution
                );

                _xenoHive.AnnounceNeedsOvipositorToSameHive(uid);
            }
        }

        var evoBonus = FixedPoint2.Zero;
        var bonuses = EntityQueryEnumerator<EvolutionBonusComponent>();
        while (bonuses.MoveNext(out var comp))
        {
            evoBonus += comp.Amount;
        }

        FixedPoint2? evoOverride = null;
        var overrides = EntityQueryEnumerator<EvolutionOverrideComponent>();
        while (overrides.MoveNext(out var comp))
        {
            evoOverride = comp.Amount;
        }

        var evolution = EntityQueryEnumerator<XenoEvolutionComponent>();
        while (evolution.MoveNext(out var uid, out var comp))
        {
            if (comp.Max == FixedPoint2.Zero)
                continue;

            if (time < comp.LastPointsAt + TimeSpan.FromSeconds(1))
                continue;

            comp.LastPointsAt = time;
            Dirty(uid, comp);

            if (!comp.GotPopup && CanEvolveAny((uid, comp)))
            {
                comp.GotPopup = true;
                Dirty(uid, comp);

                _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-ready"), uid, uid, PopupType.Large);
                _audio.PlayEntity(comp.EvolutionReadySound, uid, uid);
                continue;
            }
            var points = (_earlyEvoBoostBefore > _gameTicker.RoundDuration()) ? comp.EarlyPointsPerSecond : comp.PointsPerSecond;
            var gain = evoOverride ?? points + evoBonus;
            if (comp.Points < comp.Max || roundDuration < _evolutionAccumulatePointsBefore)
            {
                if (needsOvipositor && comp.RequiresGranter && !hasGranter)
                    continue;

                SetPoints((uid, comp), comp.Points + gain);
            }
            else if (comp.Points > comp.Max)
            {
                SetPoints((uid, comp), FixedPoint2.Max(comp.Points - gain, comp.Max));
            }
        }
    }

    private void OnXenoChangingCaste(XenoChangingCasteEvent args)
    {
        // Servers must ignore this event to prevent clients from sending it to the server,
        // because this event can cause any entity to switch over to any prototype.
        if (_net.IsServer)
            return;

        var newProto = _prototypes.Index(args.NewProtoId);
        var xeno = EntityManager.GetEntity(args.Xeno);
        if (xeno != EntityUid.Invalid)
        {
            ChangeXenoPrototype(xeno, newProto);
        }
    }

    private void ChangeXenoPrototype(
        EntityUid xeno,
        EntityPrototype newProto)
    {
        if (!_prototypes.TryIndex(CloningSettingsId, out var settings))
            return; // invalid settings

        var copyAll = settings.CopyAll;
        var excludeComponents = settings.ExcludeComponents;
        if (!copyAll)
        {
            // Xeno cloning settings should always copy all and use exclusions.
            Log.Error("Xeno cloning settings not set to copy all with exclusions. Copy failed.");
            return;
        }

        var metadata = MetaData(xeno);
        var oldProtoId = metadata.EntityPrototype?.ID;

        metadata.EntityPrototype = newProto;
        _meta.SetEntityName(xeno, newProto.Name);
        _meta.SetEntityDescription(xeno, newProto.Description);

        var registryToAdd = _serManager.CreateCopy(newProto.Components, notNullableOverride: true);
        foreach (var excluded in excludeComponents)
        {
            registryToAdd.Remove(excluded);
        }

        var additionalExclusions = new HashSet<string>();
        var changingCasteEvent = new XenoChangingPrototypeEvent(xeno, newProto, registryToAdd, additionalExclusions);
        RaiseLocalEvent(xeno, ref changingCasteEvent);
        Log.Debug("Done with XenoChangingPrototypeEvent.");

        Log.Debug("Removing additional exclusions from registry...");
        foreach (var exclusion in additionalExclusions)
        {
            if (registryToAdd.Remove(exclusion))
            {
                Log.Debug($"   Removed {exclusion}");
            }
        }
        Log.Debug("Addtional exclusion removed.");

        Log.Debug("Adding components from registry: ");
        foreach (var componentName in registryToAdd.Keys)
        {
            Log.Debug($"   {componentName}");
        }
        // Add and overwrite components that are in the registry.
        _entityManager.AddComponents(xeno, registryToAdd, true);
        Log.Debug("Added components from registry.");

        foreach (var cloneComponent in EntityManager.GetComponents(xeno))
        {
            // Remove every component that isn't in the registry or isn't excluded.
            var componentName = EntityManager.ComponentFactory.GetComponentName(cloneComponent.GetType());
            if (excludeComponents.Contains(componentName)
                || additionalExclusions.Contains(componentName)
                || registryToAdd.ContainsKey(componentName))
            {
                continue;
            }
            Log.Debug($"Removing component {componentName}...");
            RemComp(xeno, cloneComponent);
            Log.Debug($"Done removing component {componentName}.");
        }

        var afterEv = new AfterXenoChangedPrototypeEvent(xeno, oldProtoId);
        RaiseLocalEvent(xeno, ref afterEv);
    }
}
