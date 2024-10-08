using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
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
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public abstract partial class SharedXenoParasiteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly CMHandsSystem _cmHands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

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

        SubscribeLocalEvent<ParasiteSpentComponent, MapInitEvent>(OnParasiteSpentMapInit);
        SubscribeLocalEvent<ParasiteSpentComponent, UpdateMobStateEvent>(OnParasiteSpentUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
        SubscribeLocalEvent<ParasiteSpentComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<VictimInfectedComponent, MapInitEvent>(OnVictimInfectedMapInit);
        SubscribeLocalEvent<VictimInfectedComponent, ComponentRemove>(OnVictimInfectedRemoved);
        SubscribeLocalEvent<VictimInfectedComponent, ExaminedEvent>(OnVictimInfectedExamined);
        SubscribeLocalEvent<VictimInfectedComponent, RejuvenateEvent>(OnVictimInfectedRejuvenate);

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
        if (!args.CanReach || args.Target == null || args.Handled || !_cmHands.IsPickupByAllowed(ent.Owner, args.User))
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
        if (args.User != ent.Owner && !_cmHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        if (!CanInfectPopup(ent, args.Target, args.User, false))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnParasiteDragDropDragged(Entity<XenoParasiteComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.User != ent.Owner && !_cmHands.IsPickupByAllowed(ent.Owner, args.User))
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
        victim.Comp.FallOffAt = _timing.CurTime + victim.Comp.FallOffDelay;
        victim.Comp.BurstAt = _timing.CurTime + victim.Comp.BurstDelay;
    }

    private void OnVictimInfectedRemoved(Entity<VictimInfectedComponent> victim, ref ComponentRemove args)
    {
        if (_status.HasStatusEffect(victim, "Muted", null) && _status.HasStatusEffect(victim, "TemporaryBlindness", null))
        {
            _status.TryRemoveStatusEffect(victim, "Muted");
            _status.TryRemoveStatusEffect(victim, "TemporaryBlindness");
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
        if (HasComp<XenoComponent>(args.Examiner) || (CompOrNull<GhostComponent>(args.Examiner)?.CanGhostInteract ?? false))
            args.PushMarkup("This creature is impregnated.");
    }

    private void OnVictimInfectedRejuvenate(Entity<VictimInfectedComponent> victim, ref RejuvenateEvent args)
    {
        RemCompDeferred<VictimInfectedComponent>(victim);
    }

    private void OnVictimBurstMapInit(Entity<VictimBurstComponent> burst, ref MapInitEvent args)
    {
        _appearance.SetData(burst, burst.Comp.BurstLayer, true);

        if (TryComp(burst, out MobStateComponent? mobState))
            _mobState.UpdateMobState(burst, mobState);
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
        var doAfter = new DoAfterArgs(EntityManager, user, parasite.Comp.ManualAttachDelay, ev, parasite, victim)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private bool IsInfectable(EntityUid parasite, EntityUid victim)
    {
        return HasComp<InfectableComponent>(victim)
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
        }

        if (!TryRipOffClothing(victim, SlotFlags.HEAD))
            return false;
        if (!TryRipOffClothing(victim, SlotFlags.MASK, false))
            return false;

        if (_net.IsServer &&
            TryComp(victim, out InfectableComponent? infectable) &&
            TryComp(victim, out HumanoidAppearanceComponent? appearance) &&
            infectable.Sound.TryGetValue(appearance.Sex, out var sound))
        {
            var filter = Filter.Pvs(victim);
            _audio.PlayEntity(sound, filter, victim, true);
        }

        var time = _timing.CurTime;
        var victimComp = EnsureComp<VictimInfectedComponent>(victim);
        victimComp.AttachedAt = time;
        victimComp.Hive = _hive.GetHive(parasite.Owner)?.Owner;
        _stun.TryParalyze(victim, parasite.Comp.ParalyzeTime, true);
        _status.TryAddStatusEffect(victim, "Muted", parasite.Comp.ParalyzeTime, true, "Muted");
        _status.TryAddStatusEffect(victim, "TemporaryBlindness", parasite.Comp.ParalyzeTime, true, "TemporaryBlindness");
        RefreshIncubationMultipliers(victim);

        _inventory.TryEquip(victim, parasite.Owner, "mask", true, true, true);

        // TODO RMC14 also do damage to the parasite
        EnsureComp<ParasiteSpentComponent>(parasite);

        var unremovable = EnsureComp<UnremoveableComponent>(parasite);
        unremovable.DeleteOnDrop = false;
        Dirty(parasite);

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
        if (_net.IsClient)
            return;


        var time = _timing.CurTime;
        var aiQuery = EntityQueryEnumerator<ParasiteAIComponent>();
        while (aiQuery.MoveNext(out var uid, out var ai))
        {
            if (!_mobState.IsDead(uid) && !TerminatingOrDeleted(uid))
                UpdateAI((uid, ai), time);
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

        var query = EntityQueryEnumerator<VictimInfectedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var infected, out var xform))
        {
            if (infected.FallOffAt < time && !infected.FellOff)
            {
                infected.FellOff = true;
                _inventory.TryUnequip(uid, "mask", true, true, true);
            }

            if (_net.IsClient)
                continue;

            // 20 seconds before burst, spawn the larva
            if (infected.BurstAt <= time && infected.SpawnedLarva == null)
            {
                var spawned = SpawnAtPosition(infected.BurstSpawn, xform.Coordinates);
                _hive.SetHive(spawned, infected.Hive);

                var larvaContainer = _container.EnsureContainer<ContainerSlot>(uid, infected.LarvaContainerId);
                _container.Insert(spawned, larvaContainer);

                infected.CurrentStage = 6;
                Dirty(uid, infected);

                infected.SpawnedLarva = spawned;

                EnsureComp<BursterComponent>(spawned, out var burster);
                burster.BurstFrom = uid;
            }

            if (infected.BurstAt + infected.AutoBurstTime > time)
            {
                // Embryo dies if unrevivable when dead
                // Kill the embryo if we've rotted or are a simplemob
                if (_mobState.IsDead(uid) && (HasComp<InfectStopOnDeathComponent>(uid) || _rotting.IsRotten(uid)))
                {
                    if (infected.SpawnedLarva != null)
                        Burst((uid, infected));
                    else
                        RemCompDeferred<VictimInfectedComponent>(uid);
                    continue;
                }
                // Stasis slows this, while nesting makes it happen sooner
                if (infected.IncubationMultiplier != 1)
                    infected.BurstAt += TimeSpan.FromSeconds(1 - infected.IncubationMultiplier) * frameTime;

                // Stages
                // Percentage of how far along we out to burst time times the number of stages, truncated. You can't go back a stage once you've reached one
                int stage = Math.Max((int) ((infected.BurstDelay - (infected.BurstAt - time)) / infected.BurstDelay * infected.FinalStage), infected.CurrentStage);
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
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-burst-soon", ("victim", uid)), uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                    var knockdownTime = infected.BaseKnockdownTime * 75;
                    InfectionShakes(uid, infected, knockdownTime, knockdownTime, false);
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

                        var knockdownTime = infected.BaseKnockdownTime * 10;
                        InfectionShakes(uid, infected, knockdownTime, knockdownTime, false);
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
                continue;
            }

            Burst((uid, infected));

        }
    }

    // Shakes chances decrease as symptom stages progress, and they get longer
    private void InfectionShakes(EntityUid victim, VictimInfectedComponent infected, TimeSpan knockdownTime, TimeSpan jitterTime, bool popups = true)
    {
        // Don't activate when unconscious
        if (_mobState.IsIncapacitated(victim))
            return;
        //TODO Minor limb damage and causes pain
        _stun.TryParalyze(victim, knockdownTime, false);
        _status.TryAddStatusEffect(victim, "Muted", knockdownTime, true, "Muted");
        _status.TryAddStatusEffect(victim, "TemporaryBlindness", knockdownTime, true, "TemporaryBlindness");
        _jitter.DoJitter(victim, jitterTime, false);
        _damage.TryChangeDamage(victim, infected.InfectionDamage, true, false);
        if (!popups)
            return;
        _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-shakes-self"), victim, victim, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-shakes", ("victim", victim)), victim, Filter.PvsExcept(victim), true, PopupType.MediumCaution);
    }

    private void Burst(Entity<VictimInfectedComponent> burstFrom)
    {
        if (_net.IsClient)
            return;
        RemCompDeferred<VictimInfectedComponent>(burstFrom);

        var coords = _transform.GetMoverCoordinates(burstFrom);

        if (_container.TryGetContainer(burstFrom, burstFrom.Comp.LarvaContainerId, out var container))
        {
            foreach (var larva in container.ContainedEntities)
                RemCompDeferred<BursterComponent>(larva);
            _container.EmptyContainer(container, destination: coords);
        }

        Dirty(burstFrom, burstFrom.Comp);

        EnsureComp<VictimBurstComponent>(burstFrom);

        _audio.PlayPvs(burstFrom.Comp.BurstSound, burstFrom);
    }

    private void OnTryMove(Entity<BursterComponent> burster, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (TryComp<VictimInfectedComponent>(burster.Comp.BurstFrom, out var infected))
            Burst((burster.Comp.BurstFrom, infected));
    }

    /// <summary>
    ///     Tries to rip off an entity's clothing item.
    /// </summary>
    private bool TryRipOffClothing(EntityUid victim, SlotFlags slotFlags, bool doPopup = true)
    {
        if (_inventory.TryGetContainerSlotEnumerator(victim, out var slots, slotFlags))
        {
            while (slots.MoveNext(out var containerSlot))
            {
                var containedEntity = containerSlot.ContainedEntity;

                if (containedEntity != null)
                {
                    TryComp(containedEntity, out ParasiteResistanceComponent? resistance);

                    if (resistance != null && resistance.Count < resistance.MaxCount)
                    {
                        resistance.Count += 1;
                        Dirty(containedEntity.Value, resistance);

                        if (_net.IsServer && doPopup)
                        {
                            var popupMessage = Loc.GetString("rmc-xeno-infect-fail", ("target", victim), ("clothing", containedEntity));
                            _popup.PopupEntity(popupMessage, victim, PopupType.SmallCaution);
                        }

                        return false;
                    }
                    else
                    {
                        _inventory.TryUnequip(victim, victim, containerSlot.ID, force: true);

                        if (_net.IsServer && doPopup)
                        {
                            var popupMessage = Loc.GetString("rmc-xeno-infect-success", ("target", victim), ("clothing", containedEntity));
                            _popup.PopupEntity(popupMessage, victim, PopupType.MediumCaution);
                        }

                        return true;
                    }
                }
            }
        }

        return true;
    }
}
