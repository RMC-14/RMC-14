using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Egg.EggRetriever;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.Xenonids.Egg;

public sealed class XenoEggSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCHandsSystem _rmcHands = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destruction = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    private EntityQuery<StepTriggerComponent> _stepTriggerQuery;

    public override void Initialize()
    {
        _stepTriggerQuery = GetEntityQuery<StepTriggerComponent>();

        SubscribeLocalEvent<DropshipHijackStartEvent>(OnDropshipHijackStart);

        SubscribeLocalEvent<XenoComponent, XenoGrowOvipositorActionEvent>(OnXenoGrowOvipositorAction);
        SubscribeLocalEvent<XenoComponent, XenoGrowOvipositorDoAfterEvent>(OnXenoGrowOvipositorDoAfter);

        SubscribeLocalEvent<XenoAttachedOvipositorComponent, MapInitEvent>(OnXenoAttachedMapInit);
        SubscribeLocalEvent<XenoAttachedOvipositorComponent, ComponentRemove>(OnXenoAttachedRemove);
        SubscribeLocalEvent<XenoAttachedOvipositorComponent, MobStateChangedEvent>(OnXenoMobStateChanged);
        SubscribeLocalEvent<XenoAttachedOvipositorComponent, XenoConstructionRangeEvent>(OnXenoConstructionRange);

        SubscribeLocalEvent<XenoEggComponent, AfterAutoHandleStateEvent>(OnXenoEggAfterState);
        SubscribeLocalEvent<XenoEggComponent, GettingPickedUpAttemptEvent>(OnXenoEggPickedUpAttempt);
        SubscribeLocalEvent<XenoEggComponent, UseInHandEvent>(OnXenoEggUseInHand);
        SubscribeLocalEvent<XenoEggComponent, InteractUsingEvent>(OnXenoEggInteractUsing);
        SubscribeLocalEvent<XenoEggComponent, XenoEggReturnParasiteDoAfterEvent>(OnXenoEggReturnParasiteDoAfter);
        SubscribeLocalEvent<XenoEggComponent, AfterInteractEvent>(OnXenoEggAfterInteract);
        SubscribeLocalEvent<XenoEggComponent, XenoEggPlaceDoAfterEvent>(OnXenoEggPlaceDoAfter);
        SubscribeLocalEvent<XenoEggComponent, ActivateInWorldEvent>(OnXenoEggActivateInWorld);
        SubscribeLocalEvent<XenoEggComponent, StepTriggerAttemptEvent>(OnXenoEggStepTriggerAttempt);
        SubscribeLocalEvent<XenoEggComponent, StepTriggeredOffEvent>(OnXenoEggStepTriggered);
        SubscribeLocalEvent<XenoEggComponent, BeforeDamageChangedEvent>(OnXenoEggBeforeDamageChanged);
        SubscribeLocalEvent<XenoEggComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<XenoEggComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<XenoFragileEggComponent, ComponentShutdown>(OnFragileConvert);
        SubscribeLocalEvent<XenoFragileEggComponent, RefreshNameModifiersEvent>(OnFragileRefreshModifier);
        SubscribeLocalEvent<XenoFragileEggComponent, EntityTerminatingEvent>(OnFragileDelete);

        SubscribeLocalEvent<XenoEggSustainerComponent, EntityTerminatingEvent>(OnEggSustainerDelete);
        SubscribeLocalEvent<XenoEggSustainerComponent, MobStateChangedEvent>(OnEggSustainerDeath);
    }

    private void OnDropshipHijackStart(ref DropshipHijackStartEvent ev)
    {
        var query = EntityQueryEnumerator<XenoOvipositorCapableComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            foreach (var action in _rmcActions.GetActionsWithEvent<XenoGrowOvipositorActionEvent>(uid))
            {
                _actions.ClearCooldown(action.AsNullable());
            }
        }
    }

    private void OnXenoGrowOvipositorAction(Entity<XenoComponent> xeno, ref XenoGrowOvipositorActionEvent args)
    {
        if (args.Handled)
            return;

        var hasOvipositor = HasComp<XenoAttachedOvipositorComponent>(xeno);
        if (!hasOvipositor &&
            !_plasma.HasPlasmaPopup(xeno.Owner, args.AttachPlasmaCost))
        {
            return;
        }

        args.Handled = true;

        var ev = new XenoGrowOvipositorDoAfterEvent { PlasmaCost = args.AttachPlasmaCost };
        var delay = args.AttachDoAfter;
        var popup = new LocId("cm-xeno-ovipositor-attach");
        var popupType = PopupType.Medium;
        if (hasOvipositor)
        {
            ev.PlasmaCost = FixedPoint2.Zero;
            delay = args.DetachDoAfter;
            popup = "cm-xeno-ovipositor-detach";
            popupType = PopupType.MediumCaution;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, xeno, delay, ev, xeno)
        {
            BreakOnMove = true,
            MovementThreshold = 0.001f,
            BreakOnRest = !hasOvipositor,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            _popup.PopupClient(Loc.GetString(popup), xeno, xeno, popupType);
    }

    private void OnXenoGrowOvipositorDoAfter(Entity<XenoComponent> xeno, ref XenoGrowOvipositorDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            !_plasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
        {
            return;
        }

        args.Handled = true;

        if (TryComp(xeno, out XenoAttachedOvipositorComponent? attached))
            DetachOvipositor((xeno, attached));
        else
            AttachOvipositor(xeno.Owner);
    }

    private void OnXenoAttachedMapInit(Entity<XenoAttachedOvipositorComponent> attached, ref MapInitEvent args)
    {
        if (TryComp(attached, out TransformComponent? xform))
            _transform.AnchorEntity(attached, xform);

        var ev = new XenoOvipositorChangedEvent(true);
        RaiseLocalEvent(attached, ref ev, true);
    }

    private void OnXenoAttachedRemove(Entity<XenoAttachedOvipositorComponent> attached, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(attached) && TryComp(attached, out TransformComponent? xform))
        {
            _transform.Unanchor(attached, xform);
            _physics.TrySetBodyType(attached, BodyType.KinematicController);
        }

        var ev = new XenoOvipositorChangedEvent(false);
        RaiseLocalEvent(attached, ref ev, true);
    }

    private void OnXenoMobStateChanged(Entity<XenoAttachedOvipositorComponent> ent, ref MobStateChangedEvent args)
    {
        DetachOvipositor(ent);
    }

    private void OnXenoConstructionRange(Entity<XenoAttachedOvipositorComponent> ent, ref XenoConstructionRangeEvent args)
    {
        args.Range = 0;
    }

    private void OnXenoEggAfterState(Entity<XenoEggComponent> egg, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoEggStateChangedEvent();
        RaiseLocalEvent(egg, ref ev);
    }

    private void OnXenoEggPickedUpAttempt(Entity<XenoEggComponent> egg, ref GettingPickedUpAttemptEvent args)
    {
        if (egg.Comp.State != XenoEggState.Item)
            args.Cancel();
    }

    private void OnXenoEggUseInHand(Entity<XenoEggComponent> egg, ref UseInHandEvent args)
    {
        var ev = new XenoEggUseInHandEvent(_entities.GetNetEntity(egg.Owner));
        RaiseLocalEvent(args.User, ev);
        args.Handled = ev.Handled;
    }

    private void OnXenoEggAfterInteract(Entity<XenoEggComponent> egg, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (egg.Comp.State != XenoEggState.Item || !HasComp<TransformComponent>(egg))
            return;

        if ((!HasComp<EggPlantingDistanceComponent>(args.User) && !args.CanReach) ||
            (TryComp<EggPlantingDistanceComponent>(args.User, out var plantDis) && !_interaction.InRangeUnobstructed(args.User, args.ClickLocation, plantDis.Distance)))
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupCoordinates(Loc.GetString("cm-xeno-cant-reach-there"), args.ClickLocation, Filter.Local(), true);

            return;
        }

        if (!CanPlaceEggPopup(args.User, egg, args.ClickLocation, args.Handled, out _))
        {
            args.Handled = true;
            return;
        }

        args.Handled = true;

        var plantTime = TimeSpan.FromSeconds(3.5);
        if (TryComp<EggPlantTimeComponent>(args.User, out var time))
            plantTime = time.PlantTime;

        var ev = new XenoEggPlaceDoAfterEvent(GetNetCoordinates(args.ClickLocation));
        var doAfter = new DoAfterArgs(EntityManager, args.User, plantTime, ev, egg)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            RootEntity = true
        };

        _popup.PopupPredicted(Loc.GetString("rmc-xeno-egg-plant-self"), Loc.GetString("rmc-xeno-egg-plant", ("user", args.User)), egg, args.User);

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoEggPlaceDoAfter(Entity<XenoEggComponent> egg, ref XenoEggPlaceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!CanPlaceEggPopup(args.User, egg, coordinates, false, out var hiveweeds))
            return;

        if (!_plasma.TryRemovePlasmaPopup(args.User, 30))
            return;

        //Eggsac code
        if (!hiveweeds)
            EggsacSustain(args.User, egg);

        // Hand code is god-awful and its reach distance is inconsistent with args.CanReach
        // so we need to set the position ourselves.
        _transform.SetCoordinates(egg, EntityManager.GetCoordinates(args.Coordinates));
        _transform.SetLocalRotation(egg, 0);

        SetEggState(egg, XenoEggState.Growing);
        _transform.AnchorEntity(egg, Transform(egg));
        _audio.PlayPredicted(egg.Comp.PlantSound, egg, args.User);
    }

    private void EggsacSustain(EntityUid user, Entity<XenoEggComponent> egg)
    {
        SetEggSprite(egg, egg.Comp.SustainedSprite);
        if (_net.IsClient)
            return;

        //Means we must have the eggSustainer comp
        if (!TryComp<XenoEggSustainerComponent>(user, out var sustainer))
            return;

        var fragile = EnsureComp<XenoFragileEggComponent>(egg);
        fragile.SustainedBy = user;
        fragile.SustainRange = sustainer.SustainedEggsRange;
        fragile.ExpireAt = _timing.CurTime + sustainer.SustainedEggMaxTime;
        fragile.ShortExpireAt = _timing.CurTime + fragile.SustainDuration;
        fragile.CheckSustainAt = _timing.CurTime + fragile.SustainCheckEvery;
        _nameModifier.RefreshNameModifiers(egg.Owner);
        Dirty(egg, fragile);

        sustainer.SustainedEggs.Add(egg);
        if (sustainer.SustainedEggs.Count > sustainer.MaxSustainedEggs)
        {
            var decayEgg = sustainer.SustainedEggs[0];
            sustainer.SustainedEggs.Remove(decayEgg);
            if (TryComp<XenoFragileEggComponent>(decayEgg, out var fragileDecay) && TryComp<XenoEggComponent>(decayEgg, out var eggDecay))
            {
                UnsustainEgg(decayEgg, eggDecay, fragileDecay, true);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-sustain-egg-decaying", ("max", sustainer.MaxSustainedEggs)), user, user, PopupType.SmallCaution);
            }
        }
    }

    private void OnXenoEggActivateInWorld(Entity<XenoEggComponent> egg, ref ActivateInWorldEvent args)
    {
        // Prevent attempt to open the egg during a UseInHand Event
        if (!TryComp(egg.Owner, out TransformComponent? transformComp) ||
            !transformComp.Anchored)
        {
            return;
        }

        // TODO RMC14 multiple hive support
        if (!HasComp<XenoParasiteComponent>(args.User) && (!HasComp<XenoComponent>(args.User) || !HasComp<HandsComponent>(args.User)))
            return;

        if (Open(egg, args.User, out _))
            args.Handled = true;
    }

    private void OnXenoEggInteractUsing(Entity<XenoEggComponent> egg, ref InteractUsingEvent args)
    {
        var user = args.User;
        var used = args.Used;

        // Doesn't check hive or if a xeno is doing it
        if (!HasComp<XenoParasiteComponent>(used) || !_rmcHands.IsPickupByAllowed(args.Used, user))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!CanReturnParasitePopup(user, used, egg))
            return;

        var plantTime = TimeSpan.FromSeconds(3.5);
        if (TryComp<EggPlantTimeComponent>(args.User, out var time))
            plantTime = time.PlantTime;

        // this has no doafter in 13 but also the egg is not instantly able to infect when you do
        // The steptrigger works a bit diff
        var ev = new XenoEggReturnParasiteDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, plantTime, ev, egg, egg, used)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };
        _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-return-start"), args.User, args.User);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoEggReturnParasiteDoAfter(Entity<XenoEggComponent> egg, ref XenoEggReturnParasiteDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        if (!CanReturnParasitePopup(args.User, used, egg))
            return;

        _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-return-user"), args.User, args.User);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-return", ("user", args.User), ("parasite", args.Used)), egg, Filter.PvsExcept(args.User), true);

        SetEggState(egg, XenoEggState.Grown);
        QueueDel(args.Used);
    }

    private void OnXenoEggStepTriggerAttempt(Entity<XenoEggComponent> egg, ref StepTriggerAttemptEvent args)
    {
        if (CanTrigger(args.Tripper))
            args.Continue = true;
    }

    private void OnXenoEggStepTriggered(Entity<XenoEggComponent> egg, ref StepTriggeredOffEvent args)
    {
        TryTrigger(egg, args.Tripper);
    }

    private void OnGetVerbs(Entity<XenoEggComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        var uid = args.User;

        // if it doesn't have an actor and we can't reach it then don't add the verb
        if (!HasComp<ActorComponent>(uid) || !HasComp<GhostComponent>(uid))
            return;

        if (ent.Comp.State != XenoEggState.Grown)
            return;

        if (TryComp<XenoFragileEggComponent>(ent, out var fragile) && fragile.SustainedBy != null)
            return;

        var parasiteVerb = new ActivationVerb
        {
            Text = Loc.GetString("rmc-xeno-egg-ghost-verb"),
            Act = () =>
            {
                _ui.TryOpenUi(ent.Owner, XenoParasiteGhostUI.Key, uid);
            },

            Impact = LogImpact.High,
        };

        args.Verbs.Add(parasiteVerb);
    }

    private bool CanTrigger(EntityUid user)
    {
        return TryComp<InfectableComponent>(user, out var infected)
               && !infected.BeingInfected
               && !_mobState.IsDead(user)
               && !HasComp<VictimInfectedComponent>(user);
    }

    public bool Open(Entity<XenoEggComponent> egg, EntityUid? user, out EntityUid? spawned)
    {
        spawned = null;

        if (egg.Comp.State == XenoEggState.Opening)
            return false;

        if (egg.Comp.State == XenoEggState.Opened)
        {
            if (HasComp<XenoParasiteComponent>(user))
            {
                if (_mobState.IsDead(user.Value))
                    return true;

                SetEggState(egg, XenoEggState.Grown);

                if (_net.IsClient)
                    return true;

                _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-return-self", ("parasite", user)), egg);

                QueueDel(user);

                return true;
            }
            else
            {
                if (user != null)
                    _popup.PopupClient(Loc.GetString("cm-xeno-egg-clear"), egg, user.Value);

                if (_net.IsClient)
                    return true;

                QueueDel(egg);

                return true;
            }
        }

        if (HasComp<XenoParasiteComponent>(user))
        {
            if (egg.Comp.State == XenoEggState.Grown || egg.Comp.State == XenoEggState.Growing)
                _popup.PopupClient(Loc.GetString("rmc-xeno-egg-has-child"), user.Value);
            return true;
        }

        if (egg.Comp.State != XenoEggState.Grown)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-not-developed"), egg, user.Value);

            return false;
        }

        SetEggState(egg, XenoEggState.Opening);

        if (_timing.IsFirstTimePredicted)
            _audio.PlayPredicted(egg.Comp.OpenSound, egg, user);

        if (_net.IsClient)
            return true;

        var eggContainer = _container.EnsureContainer<ContainerSlot>(egg.Owner, egg.Comp.CreatureContainerId);
        spawned = SpawnInContainerOrDrop(egg.Comp.Spawn, egg.Owner, eggContainer.ID);

        _hive.SetSameHive(egg.Owner, spawned.Value);

        egg.Comp.SpawnedCreature = spawned;
        Dirty(egg);

        // TODO: create EggHatchedEvent to uncouple it from ai?
        if (TryComp<ParasiteAIComponent>(spawned, out var ai))
            _parasite.GoIdle((spawned.Value, ai));

        return true;
    }

    private void SetEggState(Entity<XenoEggComponent> egg, XenoEggState state)
    {
        egg.Comp.State = state;
        Dirty(egg);

        if (state == XenoEggState.Opened)
            RemCompDeferred<XenoFriendlyComponent>(egg);

        UpdateEggSprite(egg);

        switch (state)
        {
            case XenoEggState.Item:
            {
                if (!egg.Comp.GrownFixtures)
                    break;

                egg.Comp.GrownFixtures = false;
                Dirty(egg);

                if (_fixture.GetFixtureOrNull(egg, egg.Comp.GrowingLayerFixture) is { } fixture)
                    _physics.SetCollisionLayer(egg, egg.Comp.GrowingLayerFixture, fixture, 0);

                _fixture.DestroyFixture(egg, egg.Comp.GrowingMaskFixture);
                break;
            }
            case XenoEggState.Growing:
            case XenoEggState.Grown:
            case XenoEggState.Opening:
            case XenoEggState.Opened:
            case XenoEggState.Fragile:
            case XenoEggState.Sustained:
            {
                if (egg.Comp.GrownFixtures)
                    break;

                egg.Comp.GrownFixtures = true;
                Dirty(egg);

                _fixture.TryCreateFixture(egg, egg.Comp.GrowingMaskShape, egg.Comp.GrowingMaskFixture, hard: false, collisionMask: (int) egg.Comp.GrowingMask);

                if (_fixture.GetFixtureOrNull(egg, egg.Comp.GrowingLayerFixture) is { } fixture)
                    _physics.SetCollisionLayer(egg, egg.Comp.GrowingLayerFixture, fixture, (int) egg.Comp.GrowingLayer);

                break;
            }
        }
    }

    private void SetEggSprite(Entity<XenoEggComponent> egg, string sprite)
    {
        egg.Comp.CurrentSprite = sprite;
        Dirty(egg);

        UpdateEggSprite(egg);
    }

    private void UpdateEggSprite(Entity<XenoEggComponent> egg)
    {
        var ev = new XenoEggStateChangedEvent();
        RaiseLocalEvent(egg, ref ev);
    }

    private void AttachOvipositor(Entity<XenoAttachedOvipositorComponent?> xeno)
    {
        if (EnsureComp<XenoAttachedOvipositorComponent>(xeno, out var attached))
            return;

        xeno.Comp = attached;
        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoGrowOvipositorActionComponent? action))
            {
                _actions.SetCooldown(actionId, action.AttachCooldown);
                _actions.SetToggled(actionId, true);
            }
        }

        if (TryComp(xeno, out XenoOvipositorCapableComponent? capable))
        {
            RemoveOvipositorActions((xeno.Owner, capable));
            foreach (var actionId in capable.ActionIds)
            {
                if (_actions.AddAction(xeno, actionId) is { } action)
                    capable.Actions[actionId] = action;
            }
        }

        EnsureComp<EggPlantingDistanceComponent>(xeno).Distance = 3.5f;
    }

    private void DetachOvipositor(Entity<XenoAttachedOvipositorComponent> xeno)
    {
        if (!RemCompDeferred<XenoAttachedOvipositorComponent>(xeno))
            return;

        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoGrowOvipositorActionComponent? action))
            {
                _actions.SetCooldown(actionId, action.DetachCooldown);
                _actions.SetToggled(actionId, false);
            }
        }

        RemoveOvipositorActions(xeno.Owner);
        _popup.PopupClient(Loc.GetString("cm-xeno-ovipositor-detach"), xeno, xeno);
        RemCompDeferred<EggPlantingDistanceComponent>(xeno);
    }

    private bool TryTrigger(Entity<XenoEggComponent> egg, EntityUid tripper)
    {
        if (egg.Comp.State != XenoEggState.Grown ||
            !CanTrigger(tripper))
        {
            return false;
        }

        if (!_interaction.InRangeUnobstructed(egg.Owner, tripper) ||
            !Open(egg, tripper, out var spawned) ||
            !TryComp(spawned, out XenoParasiteComponent? parasite))
        {
            return false;
        }

        egg.Comp.InfectTarget = tripper;
        Dirty(egg);

        return true;
    }

    private bool CanPlaceEggPopup(EntityUid user, Entity<XenoEggComponent> egg, EntityCoordinates coordinates, bool handled, out bool hasHiveWeeds)
    {
        hasHiveWeeds = false;
        if (HasComp<MarineComponent>(user))
        {
            // TODO RMC14 this should have a better filter than marine component
            if (!handled)
            {
                _hands.TryDrop(user, egg, coordinates);
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-plant-outside"), user, user);
            }

            return false;
        }

        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return false;
        }

        var tile = _map.TileIndicesFor(gridId, grid, coordinates);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        hasHiveWeeds = _weeds.IsOnHiveWeeds((gridId, grid), coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoEggComponent>(uid))
            {
                var msg = Loc.GetString("cm-xeno-egg-failed-already-there");
                _popup.PopupClient(msg, uid.Value, user, PopupType.SmallCaution);
                return false;
            }

            if (HasComp<XenoConstructComponent>(uid) ||
                _tags.HasAnyTag(uid.Value, StructureTag, AirlockTag) ||
                HasComp<StrapComponent>(uid) ||
                HasComp<XenoTunnelComponent>(uid))
            {
                var msg = Loc.GetString("cm-xeno-egg-blocked");
                _popup.PopupClient(msg, uid.Value, user, PopupType.SmallCaution);
                return false;
            }
        }

        if (_turf.IsTileBlocked(gridId, tile, Impassable | MidImpassable | HighImpassable, grid))
        {
            var msg = Loc.GetString("cm-xeno-egg-blocked");
            _popup.PopupClient(msg, coordinates, user, PopupType.SmallCaution);
            return false;
        }

        if (!hasHiveWeeds)
        {
            if (!HasComp<XenoEggSustainerComponent>(user))
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-must-hive-weeds"), user, user);
            else if (!_weeds.IsOnWeeds((gridId, grid), coordinates))
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-must-weeds"), user, user);
            else
                return true;

            return false;
        }

        return true;
    }

    private bool CanReturnParasitePopup(EntityUid user, EntityUid used, Entity<XenoEggComponent> egg)
    {
        if (_mobState.IsDead(used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-dead-child"), user, user, PopupType.SmallCaution);
            return false;
        }

        if (egg.Comp.State == XenoEggState.Growing || egg.Comp.State == XenoEggState.Grown)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-has-child"), user, user, PopupType.SmallCaution);
            return false;
        }
        else if (egg.Comp.State != XenoEggState.Opened)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-fail-return"), user, user, PopupType.SmallCaution);
            return false;
        }

        if (!HasComp<ParasiteAIComponent>(used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-awake-child", ("parasite", used)), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void OnXenoEggBeforeDamageChanged(Entity<XenoEggComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.State == XenoEggState.Item) // cannot destroy in item form
            args.Cancelled = true;
    }

    private void RemoveOvipositorActions(Entity<XenoOvipositorCapableComponent?> capable)
    {
        if (!Resolve(capable, ref capable.Comp, false))
            return;

        foreach (var action in capable.Comp.Actions)
        {
            _actions.RemoveAction(action.Value);
        }

        capable.Comp.Actions.Clear();
    }

    private void OnDestruction(Entity<XenoEggComponent> ent, ref DestructionEventArgs args)
    {
        if (_net.IsClient)
            return;

        string? proto = ent.Comp.EggDestroyed;

        if (ent.Comp.CurrentSprite == ent.Comp.FragileSprite)
            proto = ent.Comp.EggDestroyedFragile;
        else if (ent.Comp.CurrentSprite == ent.Comp.SustainedSprite)
            proto = ent.Comp.EggDestroyedSustained;

        var egg = SpawnAtPosition(proto, ent.Owner.ToCoordinates());

        _audio.PlayPvs(ent.Comp.BurstSound, egg);
    }

    private void OnFragileConvert(Entity<XenoFragileEggComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.SustainedBy != null && TryComp<XenoEggSustainerComponent>(ent.Comp.SustainedBy, out var sustain))
        {
            sustain.SustainedEggs.Remove(ent);
        }
        if (TryComp<XenoEggComponent>(ent, out var egg))
            SetEggSprite((ent, egg), egg.NormalSprite);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnFragileDelete(Entity<XenoFragileEggComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.SustainedBy != null && TryComp<XenoEggSustainerComponent>(ent.Comp.SustainedBy, out var sustain))
            sustain.SustainedEggs.Remove(ent);
    }

    private void OnFragileRefreshModifier(Entity<XenoFragileEggComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            args.AddModifier("rmc-xeno-fragile-egg-prefix");
    }

    private void UnsustainEgg(EntityUid egg, XenoEggComponent eggComp, XenoFragileEggComponent fragile, bool decay = false)
    {
        if (_net.IsClient)
            return;

        SetEggSprite((egg, eggComp), eggComp.FragileSprite);
        fragile.SustainedBy = null;
        Dirty(egg, fragile);
        if (decay && fragile.BurstAt == null)
        {
            fragile.BurstAt = _timing.CurTime + fragile.BurstDelay;
            _jitter.DoJitter(egg, fragile.BurstDelay / 2, true, 40, 8, true);
        }
    }

    private void OnEggSustainerDelete(Entity<XenoEggSustainerComponent> ent, ref EntityTerminatingEvent args)
    {
        foreach (var egg in ent.Comp.SustainedEggs)
        {
            if (!TryComp<XenoFragileEggComponent>(egg, out var frag) || !TryComp<XenoEggComponent>(egg, out var eggComp))
                continue;

            UnsustainEgg(egg, eggComp, frag);
        }
    }

    private void OnEggSustainerDeath(Entity<XenoEggSustainerComponent> ent, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        foreach (var egg in ent.Comp.SustainedEggs)
        {
            if (!TryComp<XenoFragileEggComponent>(egg, out var frag) || !TryComp<XenoEggComponent>(egg, out var eggComp))
                continue;

            UnsustainEgg(egg, eggComp, frag, true);
        }

        ent.Comp.SustainedEggs.Clear();

        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-sustain-death", ("xeno", ent)), ent, PopupType.MediumCaution);
            _audio.PlayPredicted(ent.Comp.DeathSound, ent, ent);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var oviQuery = EntityQueryEnumerator<XenoOvipositorCapableComponent, XenoAttachedOvipositorComponent, TransformComponent>();
        while (oviQuery.MoveNext(out var uid, out var capable, out var attached, out var xform))
        {
            if (attached.NextEgg == null)
            {
                attached.NextEgg = time + capable.Cooldown;
                continue;
            }

            if (time < attached.NextEgg)
                continue;

            if (TryComp(uid, out MobStateComponent? state) &&
                _mobState.IsIncapacitated(uid, state))
            {
                continue;
            }

            attached.NextEgg = time + capable.Cooldown;
            Dirty(uid, attached);

            var egg = SpawnAtPosition(capable.Spawn, xform.Coordinates.Offset(capable.Offset));
            // egg belongs to whichever hive planted it, not the queen. you can steal eggs to claim them for your hive
            _hive.SetSameHive(uid, egg);

            _transform.SetLocalRotation(egg, Angle.Zero);
        }

        var eggQuery = EntityQueryEnumerator<XenoEggComponent, TransformComponent>();
        while (eggQuery.MoveNext(out var uid, out var egg, out var xform))
        {
            if (egg.State == XenoEggState.Grown &&
                _stepTriggerQuery.TryComp(uid, out var stepTrigger) &&
                stepTrigger.CurrentlySteppedOn.Count > 0)
            {
                foreach (var current in stepTrigger.CurrentlySteppedOn)
                {
                    if (TryTrigger((uid, egg), current))
                        break;
                }
            }

            if (!xform.Anchored)
                continue;

            if (time >= egg.CheckWeedsAt)
            {
                egg.CheckWeedsAt = time + egg.CheckWeedsDelay;

                if (_transform.GetGrid(uid.ToCoordinates()) is not { } gridId ||
    !TryComp(gridId, out MapGridComponent? grid))
                {
                    continue;
                }

                if (_weeds.IsOnHiveWeeds((gridId, grid), uid.ToCoordinates()))
                {
                    if (HasComp<XenoFragileEggComponent>(uid))
                    {
                        RemCompDeferred<XenoFragileEggComponent>(uid);
                    }
                }
                else
                {
                    if (!EnsureComp<XenoFragileEggComponent>(uid, out var fragile))
                    {
                        fragile.ExpireAt = time + egg.FragileEggDuration;
                        SetEggSprite((uid, egg), egg.FragileSprite);
                        _nameModifier.RefreshNameModifiers(uid);
                    }
                }

            }

            if (egg.State == XenoEggState.Growing)
            {
                egg.GrowAt ??= time + _random.Next(egg.MinTime, egg.MaxTime);

                if (time < egg.GrowAt || egg.State != XenoEggState.Growing)
                    continue;

                SetEggState((uid, egg), XenoEggState.Grown);
            }

            if (egg.State == XenoEggState.Opening)
            {
                egg.OpenAt ??= time + egg.EggOpenTime;

                if (time < egg.OpenAt || egg.State != XenoEggState.Opening)
                    continue;

                SetEggState((uid, egg), XenoEggState.Opened);

                var coords = _transform.GetMoverCoordinates(uid);

                if (_container.TryGetContainer(uid, egg.CreatureContainerId, out var container))
                    _container.EmptyContainer(container, destination: coords);

                if (egg.SpawnedCreature == null)
                    continue;

                _jitter.DoJitter(egg.SpawnedCreature.Value, egg.CreatureExitEggJitterDuration, true, 80, 8, true);

                if (egg.InfectTarget != null)
                {
                    if (TryComp<XenoParasiteComponent>(egg.SpawnedCreature, out var para))
                    {
                        _parasite.Infect((egg.SpawnedCreature.Value, para), egg.InfectTarget.Value, force: true);
                        _stun.TryParalyze(egg.InfectTarget.Value, egg.KnockdownTime, true);
                    }
                }

                egg.InfectTarget = null;
                egg.SpawnedCreature = null;
                egg.OpenAt = null;
                Dirty(uid, egg);
            }
        }

        var fragileEggQuery = EntityQueryEnumerator<XenoFragileEggComponent>();
        while (fragileEggQuery.MoveNext(out var uid, out var fragile))
        {
            if (fragile.BurstAt != null)
            {
                if (time < fragile.BurstAt)
                    continue;

                _destruction.DestroyEntity(uid);
                continue;
            }

            if (fragile.SustainedBy != null)
            {
                if (!fragile.InRange && time >= fragile.ShortExpireAt)
                {
                    fragile.BurstAt = time + fragile.BurstDelay;
                    _jitter.DoJitter(uid, fragile.BurstDelay / 2, true, 40, 8, true);
                    continue;
                }


                if (time >= fragile.CheckSustainAt)
                {
                    fragile.CheckSustainAt = time + fragile.SustainCheckEvery;
                    if (!uid.ToCoordinates().TryDistance(EntityManager, fragile.SustainedBy.Value.ToCoordinates(), out var distance))
                        continue;

                    if (distance > fragile.SustainRange)
                        fragile.InRange = false;
                    else
                    {
                        fragile.InRange = true;
                        fragile.ShortExpireAt = time + fragile.SustainDuration;
                    }
                }
            }

            if (time < fragile.ExpireAt)
                continue;

            fragile.BurstAt = time + fragile.BurstDelay;
            _jitter.DoJitter(uid, fragile.BurstDelay / 2, true, 40, 8, true);
        }
    }
}

[Serializable, NetSerializable]
public enum XenoParasiteGhostUI
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoParasiteGhostBuiMsg() : BoundUserInterfaceMessage
{

}
