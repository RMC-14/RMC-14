using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Egg.EggRetriever;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.EggMorpher;

public sealed partial class EggMorpherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedXenoEggRetrieverSystem _eggRetriever = default!;
    [Dependency] private readonly SharedXenoParasiteThrowerSystem _thrower = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggMorpherComponent, ExaminedEvent>(OnExamineEvent);

        SubscribeLocalEvent<EggMorpherComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<EggMorpherComponent, InteractUsingEvent>(OnInteractUsing);


        SubscribeLocalEvent<EggMorpherComponent, XenoChangeParasiteReserveMessage>(OnChangeParasiteReserve);
        SubscribeLocalEvent<EggMorpherComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);

        SubscribeLocalEvent<EggMorpherComponent, StepTriggerAttemptEvent>(OnEggMorpherStepAttempt);
        SubscribeLocalEvent<EggMorpherComponent, StepTriggeredOffEvent>(OnEggMorpherStepTriggered);

        SubscribeLocalEvent<EggMorpherComponent, EggMorpherFillDoafterEvent>(OnEggMorpherFill);
    }

    private void OnExamineEvent(Entity<EggMorpherComponent> eggMorpher, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
        {
            return;
        }

        using (args.PushGroup(nameof(EggMorpherComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-construction-egg-morpher-examine", ("cur_paras", eggMorpher.Comp.CurParasites), ("max_paras", eggMorpher.Comp.MaxParasites)));
        }
    }

    private void OnInteractHand(Entity<EggMorpherComponent> eggMorpher, ref InteractHandEvent args)
    {
        if (_net.IsClient)
        {
            args.Handled = true;
            return;
        }

        var user = args.User;

        if (HasComp<SynthComponent>(user))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("rmc-species-synth-programming-prevents-use", ("user", user), ("tool", eggMorpher.Owner)), user, user, PopupType.SmallCaution);
            return;
        }

        if (HasComp<XenoParasiteComponent>(user))
        {
            args.Handled = true;

            if (eggMorpher.Comp.MaxParasites <= eggMorpher.Comp.CurParasites)
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-egg-morpher-already-full"), eggMorpher, user);
                return;
            }

            if (_mobState.IsDead(user))
                return;

            if (_net.IsClient)
                return;

            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-morpher-return-self", ("parasite", user)), eggMorpher);

            QueueDel(user);
            eggMorpher.Comp.CurParasites++;
            _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);

            return;
        }

        if (!TryCreateParasiteFromEggMorpher(eggMorpher, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-egg-morpher-no-parasites"), eggMorpher, user);
            return;
        }

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<EggMorpherComponent> eggMorpher, ref InteractUsingEvent args)
    {
        if (_net.IsClient)
        {
            args.Handled = true;
            return;
        }

        var user = args.User;
        var used = args.Used;

        if (!HasComp<XenoParasiteComponent>(used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-egg-morpher-attempt-insert-non-parasite"), eggMorpher, user);
            return;
        }

        if (!HasComp<ParasiteAIComponent>(used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-awake-child", ("parasite", used)), user, user, PopupType.SmallCaution);
            return;
        }

        if (!_mobState.IsAlive(used))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-dead-child"), eggMorpher, user);
            return;
        }

        if (eggMorpher.Comp.MaxParasites <= eggMorpher.Comp.CurParasites)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-egg-morpher-already-full"), eggMorpher, user);
            return;
        }

        args.Handled = true;
        QueueDel(used);
        eggMorpher.Comp.CurParasites++;
        _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);
    }

    private void OnChangeParasiteReserve(Entity<EggMorpherComponent> eggMorpher, ref XenoChangeParasiteReserveMessage args)
    {
        eggMorpher.Comp.ReservedParasites = args.NewReserve;

    }

    private void OnGetVerbs(Entity<EggMorpherComponent> eggMorpher, ref GetVerbsEvent<ActivationVerb> args)
    {
        var (ent, comp) = eggMorpher;
        var user = args.User;
        if (_hive.FromSameHive(user, ent))
        {
            var changeReserveVerb = new ActivationVerb()
            {
                Text = Loc.GetString("xeno-reserve-parasites-verb"),
                Act = () =>
                {
                    _ui.OpenUi(ent, XenoReserveParasiteChangeUI.Key, user);
                }
            };

            args.Verbs.Add(changeReserveVerb);
        }

        if (HasComp<ActorComponent>(user) && HasComp<GhostComponent>(user) &&
            comp.CurParasites > comp.ReservedParasites && comp.CurParasites > 0)
        {
            var parasiteVerb = new ActivationVerb
            {
                Text = Loc.GetString("rmc-xeno-egg-ghost-verb"),
                Act = () =>
                {
                    _ui.TryOpenUi(ent, XenoParasiteGhostUI.Key, user);
                },

                Impact = LogImpact.High,
            };

            args.Verbs.Add(parasiteVerb);
        }
    }

    private void OnEggMorpherStepAttempt(Entity<EggMorpherComponent> eggMorpher, ref StepTriggerAttemptEvent args)
    {
        if (CanTrigger(args.Tripper))
            args.Continue = true;
    }

    private void OnEggMorpherStepTriggered(Entity<EggMorpherComponent> eggMorpher, ref StepTriggeredOffEvent args)
    {
        TryTrigger(eggMorpher, args.Tripper);
    }

    private bool CanTrigger(EntityUid user)
    {
        return TryComp<InfectableComponent>(user, out var infected)
               && !infected.BeingInfected
               && !_mobState.IsDead(user)
               && !HasComp<VictimInfectedComponent>(user);
    }

    private bool TryTrigger(Entity<EggMorpherComponent> eggMorpher, EntityUid tripper)
    {
        if (!CanTrigger(tripper))
            return false;

        if (!_interaction.InRangeUnobstructed(eggMorpher.Owner, tripper))
            return false;

        if (!TryCreateParasiteFromEggMorpher(eggMorpher, out var spawnedParasite))
            return false;

        if (spawnedParasite != null)
        {
            var parasiteComp = EnsureComp<XenoParasiteComponent>(spawnedParasite.Value);
            _parasite.Infect((spawnedParasite.Value, parasiteComp), tripper, force: true);
        }

        return true;
    }

    private void OnEggMorpherFill(Entity<EggMorpherComponent> eggMorpher, ref EggMorpherFillDoafterEvent args)
    {
        if (!TryComp<XenoEggRetrieverComponent>(args.User, out var retrieve))
            return;

        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            if (args.Transfered)
                _popup.PopupEntity(Loc.GetString("rmc-xeno-retrieve-egg-fill-finish", ("morpher", eggMorpher), ("cur_eggs", retrieve.CurEggs), ("max_eggs", retrieve.MaxEggs)), args.User, args.User);
            return;
        }

        args.Handled = true;

        if (retrieve.CurEggs <= 0 || eggMorpher.Comp.CurParasites >= eggMorpher.Comp.MaxParasites)
            return;

        _eggRetriever.RemoveEggNoSpawn((args.User, retrieve));
        eggMorpher.Comp.CurParasites++;
        _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);
        args.Transfered = true;

        if (_net.IsServer)
            _audio.PlayEntity(eggMorpher.Comp.EggFillSound, args.User, eggMorpher);

        // Check again for a repeat
        if (retrieve.CurEggs <= 0 || eggMorpher.Comp.CurParasites >= eggMorpher.Comp.MaxParasites)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-retrieve-egg-fill-finish", ("morpher", eggMorpher), ("cur_eggs", retrieve.CurEggs), ("max_eggs", retrieve.MaxEggs)), args.User, args.User);
            args.Repeat = false;
        }
        else
            args.Repeat = true;

    }

    public void EggMorpherEmpty(Entity<EggMorpherComponent> eggMorpher, Entity<XenoParasiteThrowerComponent> xeno)
    {
        var transfer = Math.Min(eggMorpher.Comp.CurParasites, xeno.Comp.CurParasites + xeno.Comp.MaxParasites);

        _thrower.AddParasites(xeno, transfer);
        eggMorpher.Comp.CurParasites -= transfer;
        _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);
        Dirty(eggMorpher);

        _popup.PopupEntity(Loc.GetString("rmc-xeno-throw-parasite-empty", ("morpher", eggMorpher), ("cur_parasites", xeno.Comp.CurParasites), ("max_parasites", xeno.Comp.MaxParasites)), xeno, PopupType.Medium);
        _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        base.Update(frameTime);

        var curTime = _time.CurTime;
        var eggMorpherQuery = EntityQueryEnumerator<EggMorpherComponent>();
        while (eggMorpherQuery.MoveNext(out var eggMorpherEnt, out var eggMorpherComp))
        {
            if (eggMorpherComp.GrowMaxParasites <= eggMorpherComp.CurParasites)
            {
                continue;
            }

            var newSpawnTime = GetParasiteSpawnCooldown((eggMorpherEnt, eggMorpherComp)) + curTime;

            if (eggMorpherComp.NextSpawnAt < curTime)
            {
                eggMorpherComp.CurParasites++;
                _appearance.SetData(eggMorpherEnt, EggmorpherOverlayVisuals.Number, eggMorpherComp.CurParasites);
                eggMorpherComp.NextSpawnAt = newSpawnTime;
                Dirty(eggMorpherEnt, eggMorpherComp);
                continue;
            }

            if (newSpawnTime < eggMorpherComp.NextSpawnAt || eggMorpherComp.NextSpawnAt is null)
            {
                eggMorpherComp.NextSpawnAt = newSpawnTime;
            }
        }
    }

    private TimeSpan GetParasiteSpawnCooldown(Entity<EggMorpherComponent> eggMorpher)
    {
        if (_hive.GetHive(eggMorpher.Owner) is not { } hive)
        {
            return eggMorpher.Comp.StandardSpawnCooldown;
        }

        if (hive.Comp.CurrentQueen is { } curQueen &&
            HasComp<XenoAttachedOvipositorComponent>(curQueen))
        {
            return eggMorpher.Comp.OviSpawnCooldown;
        }

        return eggMorpher.Comp.StandardSpawnCooldown;
    }

    /// <summary>
    /// Will return false if client side, make popup code with this in mind
    /// </summary>
    /// <param name="eggMorpher"></param>
    /// <param name="parasite"></param>
    /// <returns></returns>
    public bool TryCreateParasiteFromEggMorpher(Entity<EggMorpherComponent> eggMorpher, out EntityUid? parasite)
    {
        parasite = null;

        var (ent, comp) = eggMorpher;
        if (comp.CurParasites <= 0)
        {
            return false;
        }
        comp.CurParasites--;
        _appearance.SetData(eggMorpher, EggmorpherOverlayVisuals.Number, eggMorpher.Comp.CurParasites);
        Dirty(eggMorpher);

        if (_net.IsClient)
        {
            parasite = null;
            return true;
        }

        parasite = SpawnAtPosition(EggMorpherComponent.ParasitePrototype, ent.ToCoordinates());
        _hive.SetSameHive(eggMorpher.Owner, parasite.Value);
        return true;
    }
}
