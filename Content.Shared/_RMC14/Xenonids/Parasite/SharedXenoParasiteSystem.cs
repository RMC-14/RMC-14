using System.Linq;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;
using Content.Shared._RMC14.Xenonids.Hide;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public abstract partial class SharedXenoParasiteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCHandsSystem _rmcHands = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    private const CollisionGroup LeapCollisionGroup = CollisionGroup.InteractImpassable;
    private const CollisionGroup ThrownCollisionGroup = CollisionGroup.InteractImpassable | CollisionGroup.BarricadeImpassable;

    protected readonly ProtoId<TagPrototype> ParasiteIsPreparingLeapProtoID = new ProtoId<TagPrototype>("RMCXenoParasitePreparingLeap");

    public override void Initialize()
    {
        SubscribeLocalEvent<InfectableComponent, ActivateInWorldEvent>(OnInfectableActivate);
        SubscribeLocalEvent<InfectableComponent, CanDropTargetEvent>(OnInfectableCanDropTarget);

        SubscribeLocalEvent<XenoParasiteComponent, XenoLeapHitEvent>(OnParasiteLeapHit);
        SubscribeLocalEvent<XenoParasiteComponent, AfterInteractEvent>(OnParasiteAfterInteract);
        SubscribeLocalEvent<XenoParasiteComponent, BeforeInteractHandEvent>(OnParasiteInteractHand);
        SubscribeLocalEvent<XenoParasiteComponent, DoAfterAttemptEvent<AttachParasiteDoAfterEvent>>(OnParasiteAttachDoAfterAttempt);
        SubscribeLocalEvent<XenoParasiteComponent, AttachParasiteDoAfterEvent>(OnParasiteAttachDoAfter);
        SubscribeLocalEvent<XenoParasiteComponent, CanDragEvent>(OnParasiteCanDrag);
        SubscribeLocalEvent<XenoParasiteComponent, CanDropDraggedEvent>(OnParasiteCanDropDragged);
        SubscribeLocalEvent<XenoParasiteComponent, DragDropDraggedEvent>(OnParasiteDragDropDragged);
        SubscribeLocalEvent<XenoParasiteComponent, ThrowItemAttemptEvent>(OnParasiteThrowAttempt);
        SubscribeLocalEvent<XenoParasiteComponent, PullAttemptEvent>(OnParasiteTryPull);
        SubscribeLocalEvent<XenoParasiteComponent, GettingPickedUpAttemptEvent>(OnParasiteTryPickup);
        SubscribeLocalEvent<XenoParasiteComponent, BeforeDamageChangedEvent>(OnParasiteBeforeDamageChanged);
        SubscribeLocalEvent<XenoParasiteComponent, XenoLeapActionEvent>(OnParasiteLeap);
        SubscribeLocalEvent<XenoParasiteComponent, XenoLeapAttemptEvent>(OnParasiteLeapAttempt);
        SubscribeLocalEvent<XenoParasiteComponent, XenoLeapDoAfterEvent>(OnParasiteLeapDoAfter);
        SubscribeLocalEvent<XenoParasiteComponent, XenoLeapStoppedEvent>(OnParasiteLeapStopped);
        SubscribeLocalEvent<XenoParasiteComponent, ThrownEvent>(OnParasiteThrown);
        SubscribeLocalEvent<XenoParasiteComponent, LandEvent>(OnParasiteLand);

        SubscribeLocalEvent<ParasiteSpentComponent, MapInitEvent>(OnParasiteSpentMapInit);
        SubscribeLocalEvent<ParasiteSpentComponent, UpdateMobStateEvent>(OnParasiteSpentUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
        SubscribeLocalEvent<ParasiteSpentComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<VictimInfectedComponent, MapInitEvent>(OnVictimInfectedMapInit);
        SubscribeLocalEvent<VictimInfectedComponent, ComponentRemove>(OnVictimInfectedRemoved);
        SubscribeLocalEvent<VictimInfectedComponent, ExaminedEvent>(OnVictimInfectedExamined);
        SubscribeLocalEvent<VictimInfectedComponent, RejuvenateEvent>(OnVictimInfectedRejuvenate);
        SubscribeLocalEvent<VictimInfectedComponent, LarvaBurstDoAfterEvent>(OnBurst);

        SubscribeLocalEvent<VictimBurstComponent, MapInitEvent>(OnVictimBurstMapInit);
        SubscribeLocalEvent<VictimBurstComponent, UpdateMobStateEvent>(OnVictimUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
        SubscribeLocalEvent<VictimBurstComponent, RejuvenateEvent>(OnVictimBurstRejuvenate);
        SubscribeLocalEvent<VictimBurstComponent, ExaminedEvent>(OnVictimBurstExamine);

        SubscribeLocalEvent<BursterComponent, MoveInputEvent>(OnTryMove);
        IntializeAI();

    }

    private void OnInfectableActivate(Entity<InfectableComponent> ent, ref ActivateInWorldEvent args)
    {
        if (TryComp(args.User, out XenoParasiteComponent? parasite) &&
            StartInfect((args.User, parasite), args.Target, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnInfectableCanDropTarget(Entity<InfectableComponent> ent, ref CanDropTargetEvent args)
    {
        if (TryComp(args.Dragged, out XenoParasiteComponent? parasite) &&
            CanInfectPopup((args.Dragged, parasite), ent, args.User, false))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnParasiteLeapHit(Entity<XenoParasiteComponent> parasite, ref XenoLeapHitEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(parasite);
        var range = TryComp<ParasiteAIComponent>(parasite, out var ai) ? ai.MaxInfectRange : parasite.Comp.InfectRange;

        if (_transform.InRange(coordinates, args.Leaping.Origin, range))
            Infect(parasite, args.Hit, false);
    }

    private void OnParasiteAfterInteract(Entity<XenoParasiteComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled || !_rmcHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        if (StartInfect(ent, args.Target.Value, args.User))
            args.Handled = true;
    }

    private void OnParasiteInteractHand(Entity<XenoParasiteComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (!IsInfectable(ent, args.Target))
            return;

        StartInfect(ent, args.Target, ent);

        args.Handled = true;
    }

    private void OnParasiteAttachDoAfterAttempt(Entity<XenoParasiteComponent> ent, ref DoAfterAttemptEvent<AttachParasiteDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target)
        {
            args.Cancel();
            return;
        }

        if (!CanInfectPopup(ent, target, ent))
            args.Cancel();
    }

    private void OnParasiteAttachDoAfter(Entity<XenoParasiteComponent> ent, ref AttachParasiteDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (Infect(ent, args.Target.Value))
            args.Handled = true;
    }

    private void OnParasiteCanDrag(Entity<XenoParasiteComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnParasiteCanDropDragged(Entity<XenoParasiteComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.User != ent.Owner && !_rmcHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        if (!CanInfectPopup(ent, args.Target, args.User, false))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnParasiteDragDropDragged(Entity<XenoParasiteComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.User != ent.Owner && !_rmcHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        StartInfect(ent, args.Target, args.User);
        args.Handled = true;
    }

    private void OnParasiteThrowAttempt(Entity<XenoParasiteComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;

        if (_net.IsClient)
            return;

        var user = args.User;
        _popup.PopupEntity(Loc.GetString("rmc-xeno-cant-throw", ("target", ent)), user, user, PopupType.SmallCaution);
    }

    private void OnParasiteTryPull(Entity<XenoParasiteComponent> ent, ref PullAttemptEvent args)
    {
        if (HasComp<ParasiteAIComponent>(ent) && !HasComp<InfectableComponent>(args.PullerUid))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-parasite-nonplayer-pull", ("parasite", ent)), ent, args.PullerUid, PopupType.SmallCaution);
            args.Cancelled = true;
        }
    }

    private void OnParasiteTryPickup(Entity<XenoParasiteComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!HasComp<ParasiteAIComponent>(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-parasite-player-pickup", ("parasite", ent)), ent, args.User, PopupType.SmallCaution);
            args.Cancel();
            return;
        }

        if (HasComp<OnFireComponent>(args.User))
        {
            _popup.PopupClient("Touching the parasite while you're on fire would burn it!", ent, args.User, PopupType.MediumCaution);
            args.Cancel();
            return;
        }
    }

    private void OnParasiteBeforeDamageChanged(Entity<XenoParasiteComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.InfectedVictim != null && !ent.Comp.FellOff) // cannot damage while infecting host
            args.Cancelled = true;
    }

    private void OnParasiteLeap(Entity<XenoParasiteComponent> ent, ref XenoLeapActionEvent args)
    {
        _tagSystem.AddTag(ent, ParasiteIsPreparingLeapProtoID);
        _rmcSprite.UpdateDrawDepth(ent);

        if (TryComp<XenoHideComponent>(ent, out var xenoHideComp) &&
            xenoHideComp.Hiding)
        {
            var ev = new XenoHideActionEvent();
            ev.Performer = ent;
            ev.Toggle = false;
            RaiseLocalEvent(ent, ev);

            foreach (var action in _rmcActions.GetActionsWithEvent<XenoHideActionEvent>(ent))
            {
                _action.SetEnabled(action.AsNullable(), false);
            }
        }
    }

    private void OnParasiteLeapAttempt(Entity<XenoParasiteComponent> ent, ref XenoLeapAttemptEvent args)
    {
        if (args.Cancelled)
        {
            _tagSystem.RemoveTag(ent, ParasiteIsPreparingLeapProtoID);
            _rmcSprite.UpdateDrawDepth(ent);

            foreach (var action in _rmcActions.GetActionsWithEvent<XenoHideActionEvent>(ent))
            {
                _action.SetEnabled(action.AsNullable(), false);
            }
            return;
        }

        var contacts = _physics.GetContactingEntities(ent);
        foreach (var contact in contacts)
        {
            // Unable to leap while underneath an airlock
            if (HasComp<DoorComponent>(contact) && !HasComp<ResinDoorComponent>(contact))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-leap-blocked"), Transform(ent).Coordinates, ent);
                args.Cancelled = true;
                return;
            }
        }
    }

    private void OnParasiteLeapDoAfter(Entity<XenoParasiteComponent> ent, ref XenoLeapDoAfterEvent args)
    {
        _tagSystem.RemoveTag(ent, ParasiteIsPreparingLeapProtoID);
        _rmcSprite.UpdateDrawDepth(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoHideActionEvent>(ent))
        {
            _action.SetEnabled(action.AsNullable(), true);
        }

        if (args.Cancelled)
            return;

        var contacts = _physics.GetContactingEntities(ent);
        EntityUid? nearestResinDoor = null;
        float? nearestResinDoorDistance = null;
        foreach (var contact in contacts)
        {
            if (HasComp<DoorComponent>(contact) && HasComp<ResinDoorComponent>(contact) &&
                _physics.TryGetDistance(ent, contact, out var contactDistance) &&
                (nearestResinDoorDistance is null || nearestResinDoorDistance > contactDistance))
            {
                nearestResinDoor = contact;
                nearestResinDoorDistance = contactDistance;
            }
        }

        if (nearestResinDoor is not null)
        {
            PreventCollideComponent collisionPreventComp = new();
            collisionPreventComp.Uid = nearestResinDoor.Value;
            AddComp(ent, collisionPreventComp);
        }

        if (TryComp(ent, out FixturesComponent? fixtures))
        {
            var fixture = fixtures.Fixtures.First();
            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, fixture.Value.CollisionMask | (int) LeapCollisionGroup);
        }
    }

    private void OnParasiteLeapStopped(Entity<XenoParasiteComponent> ent, ref XenoLeapStoppedEvent args)
    {
        RemCompDeferred<PreventCollideComponent>(ent);

        if (TryComp(ent, out FixturesComponent? fixtures))
        {
            var fixture = fixtures.Fixtures.First();
            if ((fixture.Value.CollisionMask & (int) CollisionGroup.AirlockLayer) == 0)
                return;

            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, fixture.Value.CollisionMask ^ (int) LeapCollisionGroup);
        }
    }

    private void OnParasiteThrown(Entity<XenoParasiteComponent> ent, ref ThrownEvent args)
    {
        if (TryComp(ent, out FixturesComponent? fixtures))
        {
            var fixture = fixtures.Fixtures.First();
            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, fixture.Value.CollisionMask | (int)ThrownCollisionGroup);
        }
    }

    private void OnParasiteLand(Entity<XenoParasiteComponent> ent, ref LandEvent args)
    {
        if (TryComp(ent, out FixturesComponent? fixtures))
        {
            var fixture = fixtures.Fixtures.First();
            if ((fixture.Value.CollisionMask & (int) CollisionGroup.AirlockLayer & (int) CollisionGroup.BarricadeImpassable) != 0)
                return;

            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, fixture.Value.CollisionMask ^ (int) ThrownCollisionGroup);
        }
    }

    protected virtual void ParasiteLeapHit(Entity<XenoParasiteComponent> parasite)
    {
    }

    private void OnParasiteSpentMapInit(Entity<ParasiteSpentComponent> spent, ref MapInitEvent args)
    {
        if (TryComp(spent, out MobStateComponent? mobState))
            _mobState.UpdateMobState(spent, mobState);
    }

    private void OnParasiteSpentUpdateMobState(Entity<ParasiteSpentComponent> spent, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnExamined(Entity<ParasiteSpentComponent> spent, ref ExaminedEvent args)
    {
        args.PushMarkup($"[italic]{Loc.GetString("rmc-xeno-parasite-dead", ("parasite", spent))}[/italic]");
    }

    private void OnVictimInfectedMapInit(Entity<VictimInfectedComponent> victim, ref MapInitEvent args)
    {
        victim.Comp.BurstAt = _timing.CurTime + victim.Comp.BurstDelay;
    }

    private void OnVictimInfectedRemoved(Entity<VictimInfectedComponent> victim, ref ComponentRemove args)
    {
        if (_status.HasStatusEffect(victim, "Unconscious", null))
        {
            _status.TryRemoveStatusEffect(victim, "Unconscious");
        }
        _standing.Stand(victim);
    }

    private void OnVictimInfectedCancel<T>(Entity<VictimInfectedComponent> victim, ref T args) where T : CancellableEntityEventArgs
    {
        if (victim.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnVictimInfectedExamined(Entity<VictimInfectedComponent> victim, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            args.PushMarkup("This one is hosting a sister! She will emerge in time.");

        else if (HasComp<GhostComponent>(args.Examiner))
            args.PushMarkup("This creature is infected.");
    }

    private void OnVictimInfectedRejuvenate(Entity<VictimInfectedComponent> victim, ref RejuvenateEvent args)
    {
        RemCompDeferred<VictimInfectedComponent>(victim);
    }

    private void OnVictimBurstMapInit(Entity<VictimBurstComponent> burst, ref MapInitEvent args)
    {
        _appearance.SetData(burst, BurstVisuals.Visuals, VictimBurstState.Burst);
        _unrevivable.MakeUnrevivable(burst.Owner);
    }

    private void OnVictimUpdateMobState(Entity<VictimBurstComponent> burst, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnVictimBurstRejuvenate(Entity<VictimBurstComponent> burst, ref RejuvenateEvent args)
    {
        RemCompDeferred<VictimBurstComponent>(burst);
    }

    private void OnVictimBurstExamine(Entity<VictimBurstComponent> burst, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(VictimBurstComponent)))
            args.PushMarkup($"[color=red][bold]{Loc.GetString("rmc-xeno-infected-bursted", ("victim", burst))}[/bold][/color]");
    }

    private bool StartInfect(Entity<XenoParasiteComponent> parasite, EntityUid victim, EntityUid user)
    {
        if (!CanInfectPopup(parasite, victim, user))
            return false;

        var ev = new AttachParasiteDoAfterEvent();
        var delay = parasite.Comp.ManualAttachDelay;

        if (parasite.Owner == user)
            delay = parasite.Comp.SelfAttachDelay;

        if (HasComp<TrapParasiteComponent>(parasite))
            delay = TimeSpan.Zero;

        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, parasite, victim)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private bool IsInfectable(Entity<XenoParasiteComponent> parasite, EntityUid victim)
    {
        return TryComp<InfectableComponent>(victim, out var infected)
               && parasite.Comp.InfectedVictim == null
               && !infected.BeingInfected
               && !HasComp<ParasiteSpentComponent>(parasite)
               && !HasComp<VictimInfectedComponent>(victim);
    }

    private bool CanInfectPopup(Entity<XenoParasiteComponent> parasite, EntityUid victim, EntityUid user, bool popup = true, bool force = false)
    {
        if (!IsInfectable(parasite, victim))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-xeno-failed-cant-infect", ("target", victim)), victim, user, PopupType.MediumCaution);

            return false;
        }

        if (!force
            && !HasComp<XenoNestedComponent>(victim)
            && TryComp(victim, out StandingStateComponent? standing)
            && !_standing.IsDown(victim, standing))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-xeno-failed-cant-reach", ("target", victim)), victim, user, PopupType.MediumCaution);

            return false;
        }

        if (_mobState.IsDead(victim))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-xeno-failed-target-dead"), victim, user, PopupType.MediumCaution);

            return false;
        }

        if (_mobState.IsDead(parasite))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-xeno-failed-parasite-dead"), victim, user, PopupType.MediumCaution);

            return false;
        }
        return true;
    }

    public bool Infect(Entity<XenoParasiteComponent> parasite, EntityUid victim, bool popup = true, bool force = false)
    {
        if (!CanInfectPopup(parasite, victim, parasite, popup, force))
            return false;

        if (!TryComp(victim, out InfectableComponent? infectable))
            return false;

        if (_net.IsServer)
        {
            var pos = _transform.GetWorldPosition(victim);
            _transform.SetWorldPosition(parasite, pos);

            if (TryComp<ParasiteAIComponent>(parasite, out var ai))
            {
                ai.JumpsLeft--;

                if (_random.NextFloat() < ai.IdleChance)
                    GoIdle((parasite, ai));
            }

            if (TryComp<TrapParasiteComponent>(parasite, out var trap))
                ResetTrapState((parasite.Owner, trap));
        }

        if (!TryRipOffClothing(victim, SlotFlags.HEAD))
            return false;
        if (!TryRipOffClothing(victim, SlotFlags.MASK, false))
            return false;

        if (_net.IsServer &&
            TryComp(victim, out HumanoidAppearanceComponent? appearance) &&
            infectable.Sound.TryGetValue(appearance.Sex, out var sound))
        {
            _audio.PlayPvs(sound, victim);
        }

        infectable.BeingInfected = true;
        Dirty(victim, infectable);

        _size.TryKnockOut(victim, parasite.Comp.ParalyzeTime, true);
        RefreshIncubationMultipliers(victim);

        _inventory.TryEquip(victim, parasite.Owner, "mask", true, true, true);

        var unremovable = EnsureComp<UnremoveableComponent>(parasite);
        unremovable.DeleteOnDrop = false;

        parasite.Comp.InfectedVictim = victim;
        parasite.Comp.FallOffAt = _timing.CurTime + parasite.Comp.FallOffDelay;
        Dirty(parasite);

        RemCompDeferred<RMCGibOnDeathComponent>(parasite); // No gibbing on someone's face
        RemCompDeferred<ParasiteAIComponent>(parasite);
        var ev = new XenoParasiteInfectEvent(victim, parasite.Owner);
        RaiseLocalEvent(victim, ev, true);

        ParasiteLeapHit(parasite);
        return true;
    }

    public void RefreshIncubationMultipliers(Entity<VictimInfectedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetInfectedIncubationMultiplierEvent(ent.Comp.CurrentStage);
        RaiseLocalEvent(ent, ref ev);

        var multiplier = 1f;

        foreach (var add in ev.Additions)
        {
            multiplier += add;
        }

        foreach (var multi in ev.Multipliers)
        {
            multiplier *= multi;
        }

        ent.Comp.IncubationMultiplier = multiplier;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        if (_net.IsServer)
        {
            var aiQuery = EntityQueryEnumerator<ParasiteAIComponent>();
            while (aiQuery.MoveNext(out var uid, out var ai))
            {
                if (!_mobState.IsDead(uid) && !TerminatingOrDeleted(uid))
                    UpdateAI((uid, ai), time);
            }

            var trapQuery = EntityQueryEnumerator<TrapParasiteComponent>();
            while (trapQuery.MoveNext(out var uid, out var trap))
            {
                if (trap.LeapAt > time)
                    continue;

                if (_mobState.IsDead(uid) || TerminatingOrDeleted(uid))
                    continue;

                _rmcNpc.WakeNPC(uid);

                if (trap.DisableAt > time)
                    continue;

                RemCompDeferred<TrapParasiteComponent>(uid);
            }

            var aiDelayQuery = EntityQueryEnumerator<ParasiteAIDelayAddComponent>();
            while (aiDelayQuery.MoveNext(out var uid, out var aid))
            {
                if (time > aid.TimeToAI)
                {
                    EnsureComp<ParasiteAIComponent>(uid);
                    RemCompDeferred<ParasiteAIDelayAddComponent>(uid);
                }
            }
        }

        var paraQuery = EntityQueryEnumerator<XenoParasiteComponent>();
        while (paraQuery.MoveNext(out var uid, out var para))
        {
            if (para.FallOffAt < time && !para.FellOff && para.InfectedVictim != null)
            {
                var infectedVictim = para.InfectedVictim.Value;

                if (!TryComp(infectedVictim, out InfectableComponent? infectable))
                    continue;

                para.FellOff = true;
                Dirty(uid, para);

                _inventory.TryUnequip(infectedVictim, "mask", true, true, true);

                var victimComp = EnsureComp<VictimInfectedComponent>(infectedVictim);
                SetHive((infectedVictim, victimComp), _hive.GetHive(uid)?.Owner);

                // TODO RMC14 also do damage to the parasite
                EnsureComp<ParasiteSpentComponent>(uid);

                infectable.BeingInfected = false;
                Dirty(infectedVictim, infectable);
            }
        }

        var query = EntityQueryEnumerator<VictimInfectedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var infected, out var xform))
        {
            if (_net.IsClient)
                continue;

            if (infected.BurstAt + infected.AutoBurstTime <= time && infected.SpawnedLarva != null)
            {
                TryBurst((uid, infected));
                continue;
            }
            else
            {
                if (_mobState.IsDead(uid) && (HasComp<InfectStopOnDeathComponent>(uid) || _rotting.IsRotten(uid) || _unrevivable.IsUnrevivable(uid)))
                {
                    if (infected.SpawnedLarva != null)
                        TryBurst((uid, infected));
                    else
                        RemCompDeferred<VictimInfectedComponent>(uid);
                    continue;
                }
            }

            // Stasis slows this, while nesting makes it happen sooner
            if (infected.IncubationMultiplier != 1)
                infected.BurstAt += TimeSpan.FromSeconds(1 - infected.IncubationMultiplier) * frameTime;

            // spawn the larva
            if (infected.BurstAt <= time && infected.SpawnedLarva == null)
                SpawnLarva((uid, infected), out _);

            // Stages
            // Percentage of how far along we out to burst time times the number of stages, truncated. You can't go back a stage once you've reached one
            int stage = Math.Max((int)((infected.BurstDelay - (infected.BurstAt - time)) / infected.BurstDelay * infected.FinalStage), infected.CurrentStage);
            if (stage != infected.CurrentStage)
            {
                infected.CurrentStage = stage;
                Dirty(uid, infected);
                // Refresh multipliers since some become more/less effective
                RefreshIncubationMultipliers(uid);
            }

            // Warn on the last to final stage of a burst
            if (!infected.DidBurstWarning && stage == infected.BurstWarningStart)
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-burst-soon-self"), uid, uid, PopupType.MediumCaution);

                var knockdownTime = infected.BaseKnockdownTime * 75;
                InfectionShakes(uid, infected, knockdownTime, infected.JitterTime, false);
                infected.DidBurstWarning = true;

                continue;
            }

            // Symptoms only start after the IntialSymptomStart is passed (by default, 2)
            // And continue until burst time is reached
            if (stage >= infected.BurstWarningStart)
            {
                if (_random.Prob(infected.InsanePainChance * frameTime))
                {
                    var random = _random.Pick(new List<string> { "one", "two", "three", "four", "five" });
                    var message = Loc.GetString("rmc-xeno-infection-insanepain-" + random);
                    _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);

                    var knockdownTime = infected.BaseKnockdownTime * 2;
                    var jitterTime = infected.JitterTime * 0;
                    InfectionShakes(uid, infected, knockdownTime, jitterTime, false);
                }
            }
            else if (stage >= infected.FinalSymptomsStart)
            {
                if (_random.Prob(infected.MajorPainChance * frameTime))
                {
                    var message = Loc.GetString("rmc-xeno-infection-majorpain-" + _random.Pick(new List<string> { "chest", "breathing", "heart" }));
                    _popup.PopupEntity(message, uid, uid, PopupType.SmallCaution);
                    if (_random.Prob(0.5f))
                    {
                        var ev = new VictimInfectedEmoteEvent(infected.ScreamId);
                        RaiseLocalEvent(uid, ref ev);
                    }
                }

                if (_random.Prob(infected.ShakesChance * frameTime))
                    InfectionShakes(uid, infected, infected.BaseKnockdownTime * 4, infected.JitterTime * 4);
            }
            else if (stage >= infected.MiddlingSymptomsStart)
            {
                if (_random.Prob(infected.ThroatPainChance * frameTime))
                {
                    var message = Loc.GetString("rmc-xeno-infection-throat-" + _random.Pick(new List<string> { "sore", "mucous" }));
                    _popup.PopupEntity(message, uid, uid, PopupType.SmallCaution);
                }
                // TODO 20% chance to take limb damage
                else if (_random.Prob(infected.MuscleAcheChance * frameTime))
                {
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-muscle-ache"), uid, uid, PopupType.SmallCaution);
                    if (_random.Prob(0.2f))
                        _damage.TryChangeDamage(uid, infected.InfectionDamage, true, false);
                }
                else if (_random.Prob(infected.SneezeCoughChance * frameTime))
                {
                    var emote = _random.Pick(new List<ProtoId<EmotePrototype>> { infected.SneezeId, infected.CoughId });
                    var ev = new VictimInfectedEmoteEvent(emote);
                    RaiseLocalEvent(uid, ref ev);
                }

                if (_random.Prob(infected.ShakesChance * 5 / 6 * frameTime))
                    InfectionShakes(uid, infected, infected.BaseKnockdownTime * 2, infected.JitterTime * 2);
            }
            else if (stage >= infected.InitialSymptomsStart)
            {
                if (_random.Prob(infected.MinorPainChance * frameTime))
                {
                    var message = Loc.GetString("rmc-xeno-infection-minorpain-" + _random.Pick(new List<string> { "stomach", "chest" }));
                    _popup.PopupEntity(message, uid, uid, PopupType.SmallCaution);
                }

                if (_random.Prob((infected.ShakesChance * 2 / 3) * frameTime))
                    InfectionShakes(uid, infected, infected.BaseKnockdownTime, infected.JitterTime);
            }
        }
    }

    // Shakes chances decrease as symptom stages progress, and they get longer
    private void InfectionShakes(EntityUid victim, VictimInfectedComponent infected, TimeSpan knockdownTime, TimeSpan jitterTime, bool popups = true)
    {
        // Don't activate when unconscious
        if (_mobState.IsIncapacitated(victim))
            return;

        //TODO Minor limb damage and causes pain
        _size.TryKnockOut(victim, knockdownTime, true);
        _jitter.DoJitter(victim, jitterTime, false);
        _damage.TryChangeDamage(victim, infected.InfectionDamage, true, false);

        if (!popups)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-shakes-self"), victim, victim, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-shakes", ("victim", victim)), victim, Filter.PvsExcept(victim), true, PopupType.MediumCaution);
    }

    private void OnTryMove(Entity<BursterComponent> burster, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (TryComp<VictimInfectedComponent>(burster.Comp.BurstFrom, out var infected) && !infected.IsBursting)
            TryBurst((burster.Comp.BurstFrom, infected));
    }

    private void TryBurst(Entity<VictimInfectedComponent> burstFrom)
    {
        var victim = burstFrom.Owner;
        var comp = burstFrom.Comp;

        if (comp.SpawnedLarva == null)
            return;

        if (comp.IsBursting)
            return;

        comp.IsBursting = true;
        Dirty(victim, comp);

        var spawnedLarva = comp.SpawnedLarva.Value;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, spawnedLarva, comp.BurstDoAfterDelay, new LarvaBurstDoAfterEvent(), victim, target: victim)
        {
            NeedHand = false,
            BreakOnDamage = false,
            BreakOnMove = false,
            BreakOnRest = false,
            Hidden = true,
            CancelDuplicate = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(doAfterEventArgs))
        {
            /* TODO add this
            if (_net.IsServer &&
                TryComp(victim, out InfectableComponent? infectable) &&
                TryComp(victim, out HumanoidAppearanceComponent? appearance) &&
                infectable.PreburstSound.TryGetValue(appearance.Sex, out var sound) &&
                !_mobState.IsIncapacitated(victim))
            {
                var filter = Filter.Pvs(victim);
                _audio.PlayEntity(sound, filter, victim, true);
            }
            */

            EnsureComp<VictimBurstComponent>(burstFrom);
            _appearance.SetData(burstFrom.Owner, BurstVisuals.Visuals, VictimBurstState.Bursting);

            var shakeFilter = Filter.PvsExcept(victim);
            shakeFilter.RemoveWhereAttachedEntity(HasComp<BursterComponent>); // not visible the larva

            if (_net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-burst-now-victim"), victim, victim, PopupType.MediumCaution);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-burst-soon", ("victim", victim)), victim, shakeFilter, true, PopupType.LargeCaution);
                _jitter.DoJitter(victim, comp.JitterTime / 1.2, true, 14f, 5f, true); // violent jitter
            }

            var messageLarva = Loc.GetString("rmc-xeno-infection-burst-now-xeno", ("victim", Identity.Entity(victim, EntityManager)));
            _popup.PopupClient(messageLarva, spawnedLarva, spawnedLarva, PopupType.MediumCaution);
        }
    }

    private void OnBurst(Entity<VictimInfectedComponent> ent, ref LarvaBurstDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (_net.IsClient)
            return;

        EnsureComp<VictimBurstComponent>(ent.Owner);
        _appearance.SetData(ent.Owner, BurstVisuals.Visuals, VictimBurstState.Burst);

        if (TryComp(ent.Owner, out MobStateComponent? mobState))
            _mobState.UpdateMobState(ent.Owner, mobState);

        var coords = _transform.GetMoverCoordinates(ent);

        if (_container.TryGetContainer(ent, ent.Comp.LarvaContainerId, out var container))
        {
            foreach (var larva in container.ContainedEntities)
            {
                RemCompDeferred<BursterComponent>(larva);
                var invc = EnsureComp<RMCTemporaryInvincibilityComponent>(larva);
                invc.ExpiresAt = _timing.CurTime + ent.Comp.LarvaInvincibilityTime;
                Dirty(larva, invc);
            }

            _container.EmptyContainer(container, destination: coords);
        }

        Dirty(ent);
        RemCompDeferred<VictimInfectedComponent>(ent);

        _audio.PlayPvs(ent.Comp.BurstSound, args.User);
    }

    /// <summary>
    ///     Tries to rip off an entity's clothing item.
    /// </summary>
    /// <returns>
    ///     If target should be infected.
    /// </returns>
    private bool TryRipOffClothing(EntityUid victim, SlotFlags slotFlags, bool doPopup = true)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(victim, out var slots))
            return true;

        EntityUid? rippedOffItem = null;
        while (slots.NextItem(out var containedEntity, out var inventorySlot))
        {
            if ((inventorySlot.SlotFlags & slotFlags) != 0 || _tagSystem.HasTag(containedEntity, "RipOffOnInfection"))
            {
                TryComp(containedEntity, out ParasiteResistanceComponent? resistance);

                if (resistance != null && resistance.Count < resistance.MaxCount)
                {
                    resistance.Count += 1;
                    Dirty(containedEntity, resistance);

                    if (_net.IsServer && doPopup)
                    {
                        var popupMessage = Loc.GetString("rmc-xeno-infect-fail", ("target", victim), ("clothing", containedEntity));
                        _popup.PopupEntity(popupMessage, victim, PopupType.SmallCaution);
                    }

                    return false;
                }
                else
                {
                    _inventory.TryUnequip(victim, victim, inventorySlot.Name, force: true);
                    rippedOffItem = containedEntity;
                }
            }
        }

        if (_net.IsServer && doPopup && rippedOffItem != null)
        {
            var popupMessage = Loc.GetString("rmc-xeno-infect-success", ("target", victim), ("clothing", rippedOffItem));
            _popup.PopupEntity(popupMessage, victim, PopupType.MediumCaution);
        }

        return true;
    }

    public void SetBurstSpawn(Entity<VictimInfectedComponent> burst, EntProtoId spawn)
    {
        burst.Comp.BurstSpawn = spawn;
        Dirty(burst);
    }

    public void SetBurstSound(Entity<VictimInfectedComponent> burst, SoundSpecifier sound)
    {
        burst.Comp.BurstSound = sound;
        Dirty(burst);
    }

    public void TryStartBurst(Entity<VictimInfectedComponent> burst)
    {
        SetBurstDelay(burst, TimeSpan.Zero);
        TryBurst(burst);
    }

    public void SetBurstDelay(Entity<VictimInfectedComponent> burst, TimeSpan time)
    {
        burst.Comp.BurstAt = _timing.CurTime + time;
        Dirty(burst);
    }

    public void SetHive(Entity<VictimInfectedComponent> burst, EntityUid? hive)
    {
        burst.Comp.Hive = hive;
        Dirty(burst);
    }

    public void SpawnLarva(Entity<VictimInfectedComponent> victim, out EntityUid spawned)
    {
        var larvaContainer = _container.EnsureContainer<ContainerSlot>(victim.Owner, victim.Comp.LarvaContainerId);
        spawned = SpawnInContainerOrDrop(victim.Comp.BurstSpawn, victim.Owner, larvaContainer.ID);
        LinkLarvaToVictim(victim, spawned);
    }

    public void InsertLarva(Entity<VictimInfectedComponent> victim, EntityUid spawned)
    {
        var larvaContainer = _container.EnsureContainer<ContainerSlot>(victim.Owner, victim.Comp.LarvaContainerId);
        _container.InsertOrDrop(spawned, larvaContainer);
        LinkLarvaToVictim(victim, spawned);
    }

    private void LinkLarvaToVictim(Entity<VictimInfectedComponent> victim, EntityUid spawned)
    {
        if (HasComp<XenoComponent>(spawned))
            _hive.SetHive(spawned, victim.Comp.Hive);

        victim.Comp.CurrentStage = 6;
        victim.Comp.SpawnedLarva = spawned;
        Dirty(victim);

        EnsureComp<BursterComponent>(spawned, out var burster);
        burster.BurstFrom = victim.Owner;
        Dirty(spawned, burster);
    }
}

[Serializable, NetSerializable]
public sealed partial class LarvaBurstDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Event that is raised whenever a parasite infects a mob.
/// </summary>
/// <param name="Target">The Entity who was infected</param>
/// <param name="Parasite">The Parasite who infected the Target</param>
public record struct XenoParasiteInfectEvent(EntityUid Target, EntityUid Parasite);
