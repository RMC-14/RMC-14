using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel.Detector;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Sprite;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Intel.IntelSpawnerType;

namespace Content.Shared._RMC14.Intel;

public sealed class IntelSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedXenoAcidSystem _acid = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly ARESCoreSystem _aresCore = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly char[] UppercaseLetters =
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    };

    private static readonly string[] GreekLetters =
    {
        "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho",
        "Sigma", "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega",
    };

    private static readonly (string State, LocId Color)[] FolderColors =
    {
        ("folder_red", "rmc-intel-color-red"),
        ("folder_black", "rmc-intel-color-black"),
        ("folder_blue", "rmc-intel-color-blue"),
        ("folder_yellow", "rmc-intel-color-yellow"),
        ("folder", "rmc-intel-color-white"),
    };

    private static readonly Dictionary<string, LocId> DiskColors = new()
    {
        ["RMCIntelComputerDisk1"] = "rmc-intel-color-grey",
        ["RMCIntelComputerDisk2"] = "rmc-intel-color-grey",
        ["RMCIntelComputerDisk3"] = "rmc-intel-color-white",
        ["RMCIntelComputerDisk4"] = "rmc-intel-color-white",
        ["RMCIntelComputerDisk5"] = "rmc-intel-color-white",
        ["RMCIntelComputerDisk6"] = "rmc-intel-color-green",
        ["RMCIntelComputerDisk7"] = "rmc-intel-color-green",
        ["RMCIntelComputerDisk8"] = "rmc-intel-color-red",
        ["RMCIntelComputerDisk9"] = "rmc-intel-color-red",
        ["RMCIntelComputerDisk10"] = "rmc-intel-color-red",
        ["RMCIntelComputerDisk11"] = "rmc-intel-color-blue",
        ["RMCIntelComputerDisk12"] = "rmc-intel-color-blue",
        ["RMCIntelComputerDisk13"] = "rmc-intel-color-cracked-blue",
        ["RMCIntelComputerDisk14"] = "rmc-intel-color-cracked-blue",
        ["RMCIntelComputerDisk15"] = "rmc-intel-color-bloodied-blue",
    };

    private static readonly EntProtoId<IntelTechTreeComponent> TechTreeProto = "RMCIntelTechTree";

    private static readonly EntProtoId PaperScrapProto = "RMCIntelPaperScrap";
    private static readonly EntProtoId ProgressReportProto = "RMCIntelProgressReport";
    private static readonly EntProtoId FolderProto = "RMCIntelFolder";
    private static readonly EntProtoId TechnicalManualProto = "RMCIntelTechnicalManual";
    // private static readonly EntProtoId ResearchPaperProto = "RMCIntelResearchPaper";
    // private static readonly EntProtoId VialBoxProto = "RMCIntelVialBox";

    private static readonly EntProtoId[] ExperimentalDeviceProtos =
    [
        "RMCIntelRetrieveMassSpectrometer",
        "RMCIntelRetrieveReagentScanner",
        "RMCIntelRetrieveHealthAnalyzer",
        "RMCIntelRetrieveAutopsyScanner",
    ];

    private static readonly EntProtoId[] DiskProtos =
    [
        "RMCIntelComputerDisk1",
        "RMCIntelComputerDisk2",
        "RMCIntelComputerDisk3",
        "RMCIntelComputerDisk4",
        "RMCIntelComputerDisk5",
        "RMCIntelComputerDisk6",
        "RMCIntelComputerDisk7",
        "RMCIntelComputerDisk8",
        "RMCIntelComputerDisk9",
        "RMCIntelComputerDisk10",
        "RMCIntelComputerDisk11",
        "RMCIntelComputerDisk12",
        "RMCIntelComputerDisk13",
        "RMCIntelComputerDisk14",
        "RMCIntelComputerDisk15",
    ];

    private static readonly TimeSpan PersonalCluePopupDelay = TimeSpan.FromSeconds(1.25);

    private readonly Dictionary<IntelSpawnerType, float> _paperScrapChances = new()
    {
        [Close] = 20, [Medium] = 5, [Far] = 2, [Science] = 10,
    };

    private readonly Dictionary<IntelSpawnerType, float> _progressReportChances = new()
    {
        [Close] = 10, [Medium] = 55, [Far] = 3, [Science] = 10,
    };

    private readonly Dictionary<IntelSpawnerType, float> _folderChances = new()
    {
        [Close] = 20, [Medium] = 5, [Far] = 2, [Science] = 10,
    };

    private readonly Dictionary<IntelSpawnerType, float> _technicalManualChances = new()
    {
        [Close] = 20, [Medium] = 40, [Far] = 20, [Science] = 20,
    };

    private readonly Dictionary<IntelSpawnerType, float> _diskChances = new()
    {
        [Close] = 20, [Medium] = 40, [Far] = 20, [Science] = 20,
    };

    private readonly Dictionary<IntelSpawnerType, float> _experimentalDeviceChances = new()
    {
        [Close] = 10, [Medium] = 20, [Far] = 40, [Science] = 30,
    };

    private readonly Dictionary<IntelSpawnerType, float> _researchPaperChances = new()
    {
        [Close] = 25, [Medium] = 20, [Far] = 5, [Science] = 50,
    };

    private readonly Dictionary<IntelSpawnerType, float> _vialBoxChances = new()
    {
        [Close] = 15, [Medium] = 30, [Far] = 5, [Science] = 50,
    };

    private int _paperScraps;
    private int _progressReports;
    private int _folders;
    private int _technicalManuals;
    private int _disks;
    private int _dataTerminals;
    private int _safes;
    private int _experimentalDevices;
    private int _researchPapers;
    private int _vialBoxes;
    private TimeSpan _maxProcessTime;
    private TimeSpan _announceEvery;
    private int _powerObjectiveWattsRequired;
    private int _intelHumanoidsCorpsesMax;

    private readonly Dictionary<IntelSpawnerType, List<Entity<IntelSpawnerComponent>>> _spawners = new();
    private readonly Queue<Entity<IntelRetrieveItemObjectiveComponent>> _activePositionIntels = new();
    private readonly Queue<Entity<IntelRescueSurvivorObjectiveComponent>> _activeSurvivorIntels = new();
    private readonly Queue<Entity<IntelRecoverCorpseObjectiveComponent>> _activeCorpseIntels = new();
    private readonly HashSet<Entity<IntelContainerComponent>> _nearby = new();

    private EntityQuery<IntelReadObjectiveComponent> _readObjectiveQuery;

    private static readonly EntProtoId<ARESLogTypeComponent> LogCat = "ARESTabIntelLogs";

    public override void Initialize()
    {
        _readObjectiveQuery = GetEntityQuery<IntelReadObjectiveComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);

        SubscribeLocalEvent<IntelNumberComponent, RefreshNameModifiersEvent>(OnNumberRefreshNameModifiers);
        SubscribeLocalEvent<IntelClueDetailsComponent, RefreshNameModifiersEvent>(OnClueDetailsRefreshName);

        SubscribeLocalEvent<IntelUnlocksComponent, ComponentRemove>(OnUnlocksRemove);
        SubscribeLocalEvent<IntelUnlocksComponent, EntityTerminatingEvent>(OnUnlocksRemove);

        SubscribeLocalEvent<IntelRequiresComponent, ComponentRemove>(OnRequiresRemove);
        SubscribeLocalEvent<IntelRequiresComponent, EntityTerminatingEvent>(OnRequiresRemove);

        SubscribeLocalEvent<IntelReadObjectiveComponent, UseInHandEvent>(OnReadUseInHand);
        SubscribeLocalEvent<IntelReadObjectiveComponent, IntelReadDoAfterEvent>(OnReadDoAfter);

        SubscribeLocalEvent<IntelRetrieveItemObjectiveComponent, MapInitEvent>(OnRetrieveMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<IntelRetrieveItemObjectiveComponent, ContainerGettingInsertedAttemptEvent>(OnHandPickUp);
        SubscribeLocalEvent<IntelRetrieveItemObjectiveComponent, PullAttemptEvent>(OnIntelPullAttempt);
        SubscribeLocalEvent<ActiveIntelCorpseComponent, PullAttemptEvent>(OnIntelCorpsePullAttempt);

        SubscribeLocalEvent<ViewIntelObjectivesComponent, MapInitEvent>(OnViewIntelObjectivesMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<ViewIntelObjectivesComponent, ViewIntelObjectivesActionEvent>(OnViewIntelObjectivesAction);
        SubscribeLocalEvent<ViewIntelObjectivesComponent, InteractHandEvent>(OnViewIntelObjectivesInteractHand, before: [typeof(LockSystem)]);

        SubscribeLocalEvent<IntelHasUnlockedComponent, RefreshNameModifiersEvent>(OnHasUnlockedRefreshName);

        SubscribeLocalEvent<IntelSerialComponent, MapInitEvent>(OnIntelSerialMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<IntelSerialComponent, RefreshNameModifiersEvent>(OnIntelSerialRefreshNameModifiers);
        SubscribeLocalEvent<IntelSerialComponent, ExaminedEvent>(OnIntelSerialExamined);

        SubscribeLocalEvent<IntelRecoverCorpseObjectiveOnDeathComponent, MobStateChangedEvent>(OnRescueCorpseObjectiveOnDeathChanged);

        SubscribeLocalEvent<IntelRecoverCorpseObjectiveComponent, MapInitEvent>(OnRescueCorpseObjectiveMapInit, after: [typeof(AreaSystem)]);

        SubscribeLocalEvent<IntelKnowledgeComponent, ComponentRemove>(OnKnowledgeRemove);
        SubscribeLocalEvent<IntelKnowledgeComponent, EntityTerminatingEvent>(OnKnowledgeRemove);

        SubscribeLocalEvent<IntelReadComponent, ComponentRemove>(OnReadRemove);
        SubscribeLocalEvent<IntelReadComponent, EntityTerminatingEvent>(OnReadRemove);

        SubscribeLocalEvent<IntelConsoleComponent, InteractHandEvent>(OnConsoleInteractHand, before: [typeof(LockSystem)]);
        SubscribeLocalEvent<IntelConsoleComponent, IntelSubmitDoAfterEvent>(OnConsoleSubmitDoAfter);

        SubscribeLocalEvent<IntelCluesComponent, MapInitEvent>(OnIntelCluesMapInit, after: [typeof(AreaSystem)]);

        SubscribeLocalEvent<IntelDataDiskComponent, MapInitEvent>(OnDataDiskMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<IntelDataDiskComponent, RefreshNameModifiersEvent>(OnDataDiskRefreshName);
        SubscribeLocalEvent<IntelDataTerminalComponent, MapInitEvent>(OnDataTerminalMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<IntelDataTerminalComponent, InteractHandEvent>(OnDataTerminalInteractHand, before: [typeof(LockSystem)]);
        SubscribeLocalEvent<IntelDataTerminalComponent, IntelDataTerminalPasswordInputEvent>(OnDataTerminalPasswordInput);
        SubscribeLocalEvent<IntelDataTerminalComponent, ComponentRemove>(OnDataTerminalObjectiveRemove);
        SubscribeLocalEvent<IntelDataTerminalComponent, EntityTerminatingEvent>(OnDataTerminalObjectiveRemove);
        SubscribeLocalEvent<IntelDiskReaderComponent, MapInitEvent>(OnDiskReaderMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<IntelDiskReaderComponent, InteractUsingEvent>(OnDiskReaderInteractUsing);
        SubscribeLocalEvent<IntelDiskReaderComponent, InteractHandEvent>(OnDiskReaderInteractHand, before: [typeof(LockSystem)]);
        SubscribeLocalEvent<IntelDiskReaderComponent, IntelDiskReaderKeyInputEvent>(OnDiskReaderKeyInput);
        SubscribeLocalEvent<IntelSafeObjectiveComponent, InteractHandEvent>(OnSafeInteractHand, before: [typeof(LockSystem)]);
        SubscribeLocalEvent<IntelSafeObjectiveComponent, IntelSafeCodeInputEvent>(OnSafeCodeInput);
        SubscribeLocalEvent<IntelSafeObjectiveComponent, ComponentRemove>(OnSafeObjectiveRemove);
        SubscribeLocalEvent<IntelSafeObjectiveComponent, EntityTerminatingEvent>(OnSafeObjectiveRemove);

        Subs.CVar(_config, RMCCVars.RMCIntelPaperScraps, v => _paperScraps = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelProgressReports, v => _progressReports = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelFolders, v => _folders = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelTechnicalManuals, v => _technicalManuals = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelDisks, v => _disks = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelDataTerminals, v => _dataTerminals = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelSafes, v => _safes = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelExperimentalDevices, v => _experimentalDevices = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelResearchPapers, v => _researchPapers = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelVialBoxes, v => _vialBoxes = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelMaxProcessTimeMilliseconds, v => _maxProcessTime = TimeSpan.FromMilliseconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCIntelAnnounceEveryMinutes, v => _announceEvery = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCIntelPowerObjectiveWattsRequired, v => _powerObjectiveWattsRequired = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelHumanoidCorpsesMax, v => _intelHumanoidsCorpsesMax = v, true);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _spawners.Clear();
        _activePositionIntels.Clear();
        _activeSurvivorIntels.Clear();
        _activeCorpseIntels.Clear();
        _nearby.Clear();
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        var tree = EnsureTechTree();
        tree.Comp.DoAnnouncements = true;
        Dirty(tree);
    }

    private void OnNumberRefreshNameModifiers(Entity<IntelNumberComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (TryComp(ent, out IntelClueDetailsComponent? details) &&
            !string.IsNullOrWhiteSpace(details.Label))
        {
            return;
        }

        args.AddModifier("rmc-intel-suffix", extraArgs: ("number", ent.Comp.Number));
    }

    private void OnClueDetailsRefreshName(Entity<IntelClueDetailsComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.Label))
            return;

        var loc = HasComp<IntelDataDiskComponent>(ent) || HasComp<IntelDataTerminalComponent>(ent)
            ? "rmc-intel-label-name"
            : "rmc-intel-label-name-parenthetical";

        args.AddModifier(loc, extraArgs: ("label", ent.Comp.Label));
    }

    private void OnUnlocksRemove<T>(Entity<IntelUnlocksComponent> ent, ref T args)
    {
        foreach (var unlocks in ent.Comp.Unlocks)
        {
            if (TryComp(unlocks, out IntelRequiresComponent? requires))
            {
                requires.Requires.Remove(ent);
            }
        }
    }

    private void OnRequiresRemove<T>(Entity<IntelRequiresComponent> ent, ref T args)
    {
        foreach (var requires in ent.Comp.Requires)
        {
            if (TryComp(requires, out IntelUnlocksComponent? unlocks))
            {
                unlocks.Unlocks.Remove(ent);
            }
        }
    }

    private void OnReadUseInHand(Entity<IntelReadObjectiveComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;

        if (HasComp<IntelRescueSurvivorObjectiveComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-survivor-read", ("thing", Name(ent))), ent, user);
            return;
        }

        var delay = ent.Comp.Delay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var ev = new IntelReadDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent) { BreakOnDropItem = true, NeedHand = true };
        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupClient(Loc.GetString("rmc-intel-reading-start", ("thing", Name(ent))), ent, user);
    }

    private void OnHandPickUp(EntityUid ent,
        IntelRetrieveItemObjectiveComponent component,
        ContainerGettingInsertedAttemptEvent args)
    {
        var user = args.Container.Owner;
        if (HasComp<IntelRescueSurvivorObjectiveComponent>(user))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-intel-survivor-pickup", ("thing", Name(ent))), ent, user);
        }
    }

    private void OnIntelPullAttempt(Entity<IntelRetrieveItemObjectiveComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        if (HasComp<IntelRescueSurvivorObjectiveComponent>(user))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("rmc-intel-survivor-pickup", ("thing", Name(ent))), ent, user);
        }
    }

    private void OnIntelCorpsePullAttempt(Entity<ActiveIntelCorpseComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        if (HasComp<IntelRescueSurvivorObjectiveComponent>(user))
        {
            args.Cancelled = true;

            var msg = HasComp<XenoComponent>(ent)
                ? Loc.GetString("rmc-intel-survivor-xeno-pull", ("thing", Name(ent)))
                : Loc.GetString("rmc-intel-survivor-corpse-pull", ("thing", Name(ent)));

            _popup.PopupClient(msg, ent, user);
        }
    }

    private void OnReadDoAfter(Entity<IntelReadObjectiveComponent> ent, ref IntelReadDoAfterEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        args.Handled = true;
        if (args.Cancelled)
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-reading-cancelled"), ent, user);
            return;
        }

        if (ent.Comp.State == IntelObjectiveState.Inactive)
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-reading-inactive"), ent, user);
            return;
        }

        _popup.PopupClient(Loc.GetString("rmc-intel-reading-finished", ("thing", Name(ent))), ent, user);
        if (ent.Comp.State == IntelObjectiveState.Complete)
            return;

        ent.Comp.State = IntelObjectiveState.Complete;
        Dirty(ent);

        if (_net.IsClient)
            return;

        var tree = EnsureTechTree();
        tree.Comp.Tree.Documents.Current++;
        RemoveClue(tree, ent);
        AddPoints(tree, ent.Comp.Value);

        var knowledge = EnsureComp<IntelKnowledgeComponent>(user);
        knowledge.Read.Add(ent);
        Dirty(user, knowledge);

        var read = EnsureComp<IntelReadComponent>(ent);
        read.Readers.Add(user);
        Dirty(ent, read);

        if (ShowObjectiveClues(ent, user, false) > 0)
            Timer.Spawn(PersonalCluePopupDelay, () => ShowPersonalCluesAddedPopup(user));

        if (TryComp(ent, out IntelRetrieveItemObjectiveComponent? retrieve) &&
            retrieve.State == IntelObjectiveState.Inactive)
        {
            retrieve.State = IntelObjectiveState.Active;
            Dirty(ent, retrieve);
        }

        UpdateTree(tree);
    }

    private void OnRetrieveMapInit(Entity<IntelRetrieveItemObjectiveComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.State != IntelObjectiveState.Active)
            return;

        EnsureComp<ActiveIntelPositionComponent>(ent);
    }

    private void OnViewIntelObjectivesMapInit(Entity<ViewIntelObjectivesComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AddAction)
            _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnViewIntelObjectivesAction(Entity<ViewIntelObjectivesComponent> ent, ref ViewIntelObjectivesActionEvent args)
    {
        OpenIntelObjectives(ent, ent);
    }

    private void OnViewIntelObjectivesInteractHand(Entity<ViewIntelObjectivesComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.AddAction)
            return;

        OpenIntelObjectives(ent, args.User);
        args.Handled = true;
    }

    private void OpenIntelObjectives(Entity<ViewIntelObjectivesComponent> ent, EntityUid actor)
    {
        if (_net.IsServer)
        {
            var tree = EnsureTechTree().Comp.Tree;
            ent.Comp.Tree = tree;
            ent.Comp.PersonalClues = GetPersonalClues(actor);
            Dirty(ent);
        }

        _ui.OpenUi(ent.Owner, ViewIntelObjectivesUI.Key, actor);
    }

    private void OnHasUnlockedRefreshName(Entity<IntelHasUnlockedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-intel-unlocked", extraArgs: ("unlocked", string.Join(", ", ent.Comp.Unlocked)));
    }

    private void OnIntelSerialMapInit(Entity<IntelSerialComponent> ent, ref MapInitEvent args)
    {
        int Number()
        {
            return _random.Next(0, 10);
        }

        char Char()
        {
            return _random.Pick(UppercaseLetters);
        }

        ent.Comp.Serial = $"{Number()}{Char()}{Number()}{Number()}{Number()}{Number()}{Char()}";
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnIntelSerialRefreshNameModifiers(Entity<IntelSerialComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-intel-serial-name", extraArgs: ("serial", ent.Comp.Serial));
    }

    private void OnIntelSerialExamined(Entity<IntelSerialComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(IntelSerialComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-intel-serial-examine", ("serial", ent.Comp.Serial)));
        }
    }

    private void OnRescueCorpseObjectiveOnDeathChanged(Entity<IntelRecoverCorpseObjectiveOnDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Dead || args.NewMobState != MobState.Dead)
            return;

        if (HasComp<IntelRecoverCorpseObjectiveComponent>(args.Target))
            return;

        var comp = EnsureComp<IntelRecoverCorpseObjectiveComponent>(args.Target);
        comp.Value = ent.Comp.Value;
        Dirty(args.Target, comp);
    }

    private void OnRescueCorpseObjectiveMapInit(Entity<IntelRecoverCorpseObjectiveComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<ActiveIntelCorpseComponent>(ent);
    }

    private void OnKnowledgeRemove<T>(Entity<IntelKnowledgeComponent> ent, ref T args)
    {
        foreach (var read in ent.Comp.Read)
        {
            if (TerminatingOrDeleted(read))
                continue;

            if (TryComp(read, out IntelReadComponent? readComp))
            {
                readComp.Readers.Remove(ent);
                Dirty(read, readComp);
            }
        }
    }

    private void OnReadRemove<T>(Entity<IntelReadComponent> ent, ref T args)
    {
        foreach (var reader in ent.Comp.Readers)
        {
            if (TerminatingOrDeleted(reader))
                continue;

            if (TryComp(reader, out IntelKnowledgeComponent? knowledge))
            {
                knowledge.Read.Remove(ent);
                Dirty(reader, knowledge);
            }
        }
    }

    private void OnConsoleInteractHand(Entity<IntelConsoleComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(args.User, out IntelKnowledgeComponent? knowledge) ||
            !knowledge.Read.TryFirstOrNull(out var read))
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-console-typing-no-new"), ent, args.User, PopupType.Medium);
            return;
        }

        var msg = Loc.GetString("rmc-intel-console-typing-start");
        _popup.PopupClient(msg, ent, args.User, PopupType.Medium);

        var delay = ent.Comp.Delay * _skills.GetSkillDelayMultiplier(args.User, ent.Comp.Skill);
        var ev = new IntelSubmitDoAfterEvent { Intel = GetNetEntity(read.Value) };
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, ent, ent, ent) { BreakOnMove = true };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnConsoleSubmitDoAfter(Entity<IntelConsoleComponent> ent, ref IntelSubmitDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-intel-console-typing-cancelled"),
                ent,
                args.User,
                PopupType.MediumCaution
            );

            args.Repeat = false;
            return;
        }

        void StopPopup(ref IntelSubmitDoAfterEvent args)
        {
            if (args.Amount == 0)
                _popup.PopupEntity(Loc.GetString("rmc-intel-console-submit-no-new"), ent, args.User, PopupType.Medium);
            else
                _popup.PopupEntity(Loc.GetString("rmc-intel-console-submit-done", ("amount", args.Amount)), ent, args.User, PopupType.Medium);

            if (_idCard.TryFindIdCard(args.User, out var idCard) && TryComp(idCard, out ItemIFFComponent? idCardIFF))
            {
                foreach (var faction in idCardIFF.Factions)
                {
                    _aresCore.CreateARESLog(faction, LogCat, (string)$"{Name(args.User)} processed {args.Amount} intel entries");
                }
            }
        }

        if (!TryComp(args.User, out IntelKnowledgeComponent? knowledge))
        {
            StopPopup(ref args);
            return;
        }

        if (!TryGetEntity(args.Intel, out var intel) ||
            !TryComp(intel, out IntelUnlocksComponent? unlocks) ||
            !unlocks.Unlocks.TryFirstOrNull(out var unlock))
        {
            if (!knowledge.Read.TryFirstOrNull(out intel))
            {
                StopPopup(ref args);
                return;
            }

            args.Intel = GetNetEntity(intel.Value);
            if (!TryComp(intel, out unlocks) ||
                !unlocks.Unlocks.TryFirstOrNull(out unlock))
            {
                knowledge.Read.Remove(intel.Value);
                Dirty(args.User, knowledge);
                args.Repeat = true;
                return;
            }
        }

        if (TryComp(unlock, out IntelCluesComponent? cluesComp))
            ShowClue(unlock.Value, cluesComp, args.User, true);

        unlocks.Unlocks.Remove(unlock.Value);
        Dirty(intel.Value, unlocks);
        ActivateIntel(intel.Value, unlock.Value);
        args.Amount++;
        _audio.PlayPvs(ent.Comp.TypingSound, ent);

        if (unlocks.Unlocks.Count == 0 &&
            knowledge.Read.Remove(intel.Value))
        {
            Dirty(args.User, knowledge);
        }

        if (knowledge.Read.Count > 0)
            args.Repeat = true;
        else
            StopPopup(ref args);
    }

    private int ShowObjectiveClues(EntityUid source, EntityUid user, bool storeGlobal)
    {
        if (!TryComp(source, out IntelUnlocksComponent? unlocks))
            return 0;

        var shown = 0;
        foreach (var unlock in unlocks.Unlocks)
        {
            if (TryComp(unlock, out IntelCluesComponent? clues))
                shown += ShowClue(unlock, clues, user, storeGlobal) ? 1 : 0;
        }

        return shown;
    }

    private bool ShowClue(EntityUid target, IntelCluesComponent clues, EntityUid user, bool storeGlobal)
    {
        var clue = GetClueMessage(target, clues);

        if (!storeGlobal)
            return AddPersonalClue(user, target, clue);

        _popup.PopupEntity(clue, target, user, PopupType.Medium);

        RemovePersonalClueFromAll(target);

        if (clues.Category is not { } category)
        {
            return false;
        }

        var tree = EnsureTechTree();
        var clueGroup = tree.Comp.Tree.Clues.GetOrNew(category);
        clueGroup[GetNetEntity(target)] = clue;
        Dirty(tree);
        UpdateTree(tree);
        return true;
    }

    private Dictionary<NetEntity, string> GetPersonalClues(EntityUid user)
    {
        if (!TryComp(user, out IntelKnowledgeComponent? knowledge))
            return new Dictionary<NetEntity, string>();

        return new Dictionary<NetEntity, string>(knowledge.PersonalClues);
    }

    private bool AddPersonalClue(EntityUid user, EntityUid target, string clue)
    {
        var knowledge = EnsureComp<IntelKnowledgeComponent>(user);
        var targetNet = GetNetEntity(target);
        var added = !knowledge.PersonalClues.TryGetValue(targetNet, out var existing) ||
                    existing != clue;

        knowledge.PersonalClues[targetNet] = clue;
        Dirty(user, knowledge);
        SyncPersonalClues(user, knowledge);
        return added;
    }

    private void SyncPersonalClues(EntityUid user, IntelKnowledgeComponent knowledge)
    {
        if (!TryComp(user, out ViewIntelObjectivesComponent? view))
            return;

        view.PersonalClues = new Dictionary<NetEntity, string>(knowledge.PersonalClues);
        Dirty(user, view);
    }

    private void RemovePersonalClueFromAll(EntityUid target)
    {
        var targetNet = GetNetEntity(target);
        var query = EntityQueryEnumerator<IntelKnowledgeComponent>();
        while (query.MoveNext(out var uid, out var knowledge))
        {
            if (!knowledge.PersonalClues.Remove(targetNet))
                continue;

            Dirty(uid, knowledge);
            SyncPersonalClues(uid, knowledge);
        }
    }

    private void ShowPersonalCluesAddedPopup(EntityUid user)
    {
        if (TerminatingOrDeleted(user))
            return;

        if (!TryComp(user, out IntelKnowledgeComponent? knowledge) ||
            knowledge.PersonalClues.Count == 0)
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-intel-personal-clues-added"), user, user, PopupType.Medium);
    }

    private string GetClueMessage(EntityUid target, IntelCluesComponent clues)
    {
        var args = new List<(string, object)>
        {
            ("name", GetClueName(target)),
            ("area", clues.InitialArea),
            ("label", GetClueLabel(target)),
            ("color", GetClueColor(target)),
        };

        if (TryComp(target, out IntelDataDiskComponent? disk))
            args.Add(("key", disk.EncryptionKey));

        if (TryComp(target, out IntelDataTerminalComponent? terminal))
            args.Add(("password", terminal.Password));

        if (TryComp(target, out IntelSafeObjectiveComponent? safe))
            args.Add(("code", safe.Code));

        return Loc.GetString(clues.Clue, args.ToArray());
    }

    private string GetClueName(EntityUid target)
    {
        if (MetaData(target).EntityPrototype is { } proto)
            return proto.Name;

        return Name(target);
    }

    private string GetClueLabel(EntityUid target)
    {
        if (TryComp(target, out IntelClueDetailsComponent? details) &&
            !string.IsNullOrWhiteSpace(details.Label))
        {
            return details.Label;
        }

        if (TryComp(target, out IntelNumberComponent? number) &&
            number.Number > 0)
        {
            return Loc.GetString("rmc-intel-clue-label-number", ("number", number.Number));
        }

        if (TryComp(target, out IntelSerialComponent? serial) &&
            !string.IsNullOrWhiteSpace(serial.Serial))
        {
            return Loc.GetString("rmc-intel-clue-label-serial", ("serial", serial.Serial));
        }

        return Loc.GetString("rmc-intel-clue-label-unmarked");
    }

    private string GetClueColor(EntityUid target)
    {
        if (TryComp(target, out IntelClueDetailsComponent? details) &&
            details.ColorName is { } color)
        {
            return Loc.GetString(color);
        }

        return Loc.GetString("rmc-intel-color-unknown");
    }

    private void OnIntelCluesMapInit(Entity<IntelCluesComponent> ent, ref MapInitEvent args)
    {
        SetInitialArea(ent);
    }

    private void SetInitialArea(Entity<IntelCluesComponent> ent)
    {
        if (_area.TryGetArea(ent, out var area, out _))
        {
            ent.Comp.InitialArea = Name(area.Value);
            Dirty(ent);
        }
    }

    private void OnDataDiskMapInit(Entity<IntelDataDiskComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.EncryptionKey))
            ent.Comp.EncryptionKey = GenerateAccessKey();

        var details = EnsureComp<IntelClueDetailsComponent>(ent);
        if (string.IsNullOrWhiteSpace(details.Label))
            details.Label = GenerateDataLabel();

        if (MetaData(ent).EntityPrototype is { } proto &&
            DiskColors.TryGetValue(proto.ID, out var color))
        {
            details.ColorName = color;
        }

        Dirty(ent, details);
        _nameModifier.RefreshNameModifiers(ent.Owner);
        Dirty(ent);
    }

    private void OnDataDiskRefreshName(Entity<IntelDataDiskComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.Completed)
            args.AddModifier("rmc-intel-data-disk-uploaded");
    }

    private void OnDataTerminalMapInit(Entity<IntelDataTerminalComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.Password))
            ent.Comp.Password = GenerateAccessKey();

        Dirty(ent);
    }

    private void OnDataTerminalInteractHand(Entity<IntelDataTerminalComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;

        if (_net.IsClient)
            return;

        if (ent.Comp.Completed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-complete"), ent, args.User);
            return;
        }

        if (ent.Comp.Uploading)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-uploading"), ent, args.User);
            return;
        }

        if (!CanUploadData(ent, args.User, out var reason))
        {
            _popup.PopupEntity(reason, ent, args.User, PopupType.MediumCaution);
            return;
        }

        _dialog.OpenInput(
            ent,
            args.User,
            Loc.GetString("rmc-intel-data-terminal-password-prompt"),
            new IntelDataTerminalPasswordInputEvent(GetNetEntity(args.User)),
            characterLimit: 16,
            minCharacterLimit: 1);
    }

    private void OnDataTerminalPasswordInput(Entity<IntelDataTerminalComponent> ent, ref IntelDataTerminalPasswordInputEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.User, out var user))
            return;

        if (ent.Comp.Completed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-complete"), ent, user.Value);
            return;
        }

        if (!CanUploadData(ent, user.Value, out var reason))
        {
            _popup.PopupEntity(reason, ent, user.Value, PopupType.MediumCaution);
            return;
        }

        if (!AccessMatches(args.Message, ent.Comp.Password))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-wrong-password"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Uploading = true;
        ent.Comp.LastUser = user.Value;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-started"), ent, user.Value);
    }

    private void OnDiskReaderMapInit(Entity<IntelDiskReaderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnDiskReaderInteractUsing(Entity<IntelDiskReaderComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out IntelDataDiskComponent? disk))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (disk.Completed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-disk-complete"), ent, args.User);
            return;
        }

        if (!_power.IsPowered(ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-no-power"), ent, args.User, PopupType.MediumCaution);
            return;
        }

        if (TryGetReaderDisk(ent, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-occupied"), ent, args.User, PopupType.MediumCaution);
            return;
        }

        _dialog.OpenInput(
            ent,
            args.User,
            Loc.GetString("rmc-intel-disk-reader-key-prompt"),
            new IntelDiskReaderKeyInputEvent(GetNetEntity(args.User), GetNetEntity(args.Used)),
            characterLimit: 16,
            minCharacterLimit: 1);
    }

    private void OnDiskReaderInteractHand(Entity<IntelDiskReaderComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || HasComp<IntelConsoleComponent>(ent))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!TryGetReaderDisk(ent, out var disk))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-empty"), ent, args.User);
            return;
        }

        EjectDisk(ent, disk.Value, args.User);
    }

    private void OnDiskReaderKeyInput(Entity<IntelDiskReaderComponent> ent, ref IntelDiskReaderKeyInputEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.User, out var user) ||
            !TryGetEntity(args.Disk, out var diskId) ||
            !TryComp(diskId, out IntelDataDiskComponent? disk))
        {
            return;
        }

        if (disk.Completed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-disk-complete"), ent, user.Value);
            return;
        }

        if (!_power.IsPowered(ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-no-power"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        if (TryGetReaderDisk(ent, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-occupied"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        if (!AccessMatches(args.Message, disk.EncryptionKey))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-wrong-key"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        if (!_container.Insert(diskId.Value, slot))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-insert-failed"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        disk.Uploading = true;
        disk.LastUser = user.Value;
        Dirty(diskId.Value, disk);

        ent.Comp.LastUser = user.Value;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-started"), ent, user.Value);
    }

    private void OnSafeInteractHand(Entity<IntelSafeObjectiveComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.Completed)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.Code))
        {
            ent.Comp.Code = GenerateSafeCode();
            Dirty(ent);
        }

        _dialog.OpenInput(
            ent,
            args.User,
            Loc.GetString("rmc-intel-safe-code-prompt"),
            new IntelSafeCodeInputEvent(GetNetEntity(args.User)),
            characterLimit: 4,
            minCharacterLimit: 4);
    }

    private void OnSafeCodeInput(Entity<IntelSafeObjectiveComponent> ent, ref IntelSafeCodeInputEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.User, out var user))
            return;

        if (ent.Comp.Completed)
            return;

        if (!AccessMatches(args.Message, ent.Comp.Code))
        {
            _popup.PopupEntity(Loc.GetString("rmc-intel-safe-wrong-code"), ent, user.Value, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Completed = true;
        Dirty(ent);

        var tree = EnsureTechTree();
        tree.Comp.Tree.Miscellaneous.Current++;
        RemoveClue(tree, ent);
        AddPoints(tree, ent.Comp.Value);

        _lock.Unlock(ent, user.Value);
        _entityStorage.TryOpenStorage(user.Value, ent);
        _popup.PopupEntity(Loc.GetString("rmc-intel-safe-complete"), ent, user.Value);
    }

    private void OnSafeObjectiveRemove<T>(Entity<IntelSafeObjectiveComponent> ent, ref T args)
    {
        if (_net.IsClient || ent.Comp.Completed || !TryGetTechTree(out var tree))
            return;

        tree.Value.Comp.Tree.Miscellaneous.Total = Math.Max(0, tree.Value.Comp.Tree.Miscellaneous.Total - 1);
        RemoveClue(tree.Value, ent);
        Dirty(tree.Value);
        UpdateTree(tree.Value);
    }

    private void OnDataTerminalObjectiveRemove<T>(Entity<IntelDataTerminalComponent> ent, ref T args)
    {
        if (_net.IsClient || ent.Comp.Completed || !TryGetTechTree(out var tree))
            return;

        tree.Value.Comp.Tree.UploadData.Total = Math.Max(0, tree.Value.Comp.Tree.UploadData.Total - 1);
        RemoveClue(tree.Value, ent);
        Dirty(tree.Value);
        UpdateTree(tree.Value);
    }

    private bool CanUploadData(Entity<IntelDataTerminalComponent> terminal, EntityUid user, out string reason)
    {
        if (!_power.IsPowered(terminal))
        {
            reason = Loc.GetString("rmc-intel-data-terminal-no-power");
            return false;
        }

        if (!TryGetTechTree(out var tree) ||
            !tree.Value.Comp.Tree.ColonyCommunications)
        {
            reason = Loc.GetString("rmc-intel-data-terminal-no-comms");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool TryGetReaderDisk(Entity<IntelDiskReaderComponent> reader, [NotNullWhen(true)] out Entity<IntelDataDiskComponent>? disk)
    {
        disk = null;
        if (!_container.TryGetContainer(reader, reader.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var first) ||
            !TryComp(first.Value, out IntelDataDiskComponent? diskComp))
        {
            return false;
        }

        disk = (first.Value, diskComp);
        return true;
    }

    private void EjectDisk(Entity<IntelDiskReaderComponent> reader, Entity<IntelDataDiskComponent> disk, EntityUid? user)
    {
        if (_container.TryGetContainer(reader, reader.Comp.ContainerId, out var container))
            _container.Remove(disk.Owner, container);

        disk.Comp.Uploading = false;
        Dirty(disk);

        if (user is { Valid: true } userId && _hands.TryPickupAnyHand(userId, disk.Owner))
            return;

        _transform.DropNextTo(disk.Owner, reader.Owner);
    }

    private void CompleteUpload(EntityUid target, FixedPoint2 value)
    {
        var tree = EnsureTechTree();
        tree.Comp.Tree.UploadData.Current++;
        RemoveClue(tree, target);
        AddPoints(tree, value);
    }

    private void RemoveClue(Entity<IntelTechTreeComponent> tree, EntityUid target)
    {
        RemovePersonalClueFromAll(target);

        if (!TryComp(target, out IntelCluesComponent? clues) ||
            clues.Category is not { } category ||
            !tree.Comp.Tree.Clues.TryGetValue(category, out var clueGroup))
        {
            return;
        }

        clueGroup.Remove(GetNetEntity(target));
        Dirty(tree);
    }

    private bool AccessMatches(string input, string expected)
    {
        return string.Equals(NormalizeAccess(input), NormalizeAccess(expected), StringComparison.OrdinalIgnoreCase);
    }

    private string NormalizeAccess(string value)
    {
        return value.Trim().Replace(" ", string.Empty);
    }

    private string GenerateAccessKey()
    {
        return $"{RandomUppercase()}{_random.Next(100, 1000)}{RandomUppercase()}{_random.Next(10, 100)}";
    }

    private string GenerateDocumentLabel()
    {
        return $"{RandomUppercase()}{_random.Next(100, 1000)}";
    }

    private string GenerateDataLabel()
    {
        return $"{_random.Pick(GreekLetters)}-{_random.Next(100, 1000)}";
    }

    private string GenerateSafeCode()
    {
        return _random.Next(1000, 10000).ToString();
    }

    private char RandomUppercase()
    {
        return _random.Pick(UppercaseLetters);
    }

    private int RandomDigit()
    {
        return _random.Next(0, 10);
    }

    private List<EntityUid> SpawnIntel(EntProtoId proto, int count, Dictionary<IntelSpawnerType, float> chances)
    {
        return SpawnIntel([proto], count, chances);
    }

    private List<EntityUid> SpawnIntel(
        IReadOnlyList<EntProtoId> protos,
        int count,
        Dictionary<IntelSpawnerType, float> chances,
        bool activePosition = true,
        bool randomNumber = true,
        bool insertNearby = true,
        Action<EntityUid>? onSpawn = null)
    {
        var items = new List<EntityUid>();
        if (protos.Count == 0)
            return items;

        for (var i = 0; i < count; i++)
        {
            var type = _random.Pick(chances);
            if (!_spawners.TryGetValue(type, out var spawners) ||
                spawners.Count <= 0)
            {
                continue;
            }

            var spawner = _random.Pick(spawners);
            var coords = _transform.GetMoverCoordinates(spawner);
            var proto = _random.Pick(protos);
            var intel = Spawn(proto, coords);
            items.Add(intel);

            if (activePosition)
                EnsureComp<ActiveIntelPositionComponent>(intel);

            if (randomNumber)
            {
                var number = EnsureComp<IntelNumberComponent>(intel);
                number.Number = _random.Next(100, 1000);
                Dirty(intel, number);
                _nameModifier.RefreshNameModifiers(intel);
            }

            onSpawn?.Invoke(intel);

            if (!insertNearby)
                continue;

            _nearby.Clear();
            _entityLookup.GetEntitiesInRange(coords, 0.5f, _nearby, LookupFlags.Uncontained);

            foreach (var nearby in _nearby)
            {
                if (HasComp<StorageComponent>(nearby) &&
                    _storage.Insert(nearby, intel, out _))
                {
                    break;
                }
                else if (_entityStorage.Insert(intel, nearby))
                {
                    break;
                }
            }
        }

        return items;
    }

    public Entity<IntelTechTreeComponent> EnsureTechTree()
    {
        if (TryGetTechTree(out var tree))
            return tree.Value;

        var treeId = Spawn(TechTreeProto);
        var treeComp = EnsureComp<IntelTechTreeComponent>(treeId);
        tree = (treeId, treeComp);

        foreach (var tier in treeComp.Tree.Options)
        {
            for (var i = 0; i < tier.Count; i++)
            {
                var option = tier[i];
                if (option.CurrentCost == 0)
                    tier[i] = option with { CurrentCost = option.Cost };
            }
        }

        return tree.Value;
    }

    public bool TryGetTechTree([NotNullWhen(true)] out Entity<IntelTechTreeComponent>? tree)
    {
        var techTreeQuery = EntityQueryEnumerator<IntelTechTreeComponent>();
        while (techTreeQuery.MoveNext(out var uid, out var comp))
        {
            tree = (uid, comp);
            return true;
        }

        tree = default;
        return false;
    }

    public void RunSpawners()
    {
        try
        {
            var spawnerQuery = EntityQueryEnumerator<IntelSpawnerComponent>();
            while (spawnerQuery.MoveNext(out var uid, out var comp))
            {
                if (EntityManager.IsQueuedForDeletion(uid))
                    continue;

                _spawners.GetOrNew(comp.IntelType).Add((uid, comp));
            }

            if (_spawners.Count == 0)
                return;

            foreach (var spawners in _spawners.Values)
            {
                foreach (var spawner in spawners)
                {
                    QueueDel(spawner);
                }
            }

            var tree = EnsureTechTree();
            var lows = SpawnIntel(PaperScrapProto, _paperScraps, _paperScrapChances);
            var reports = SpawnIntel(ProgressReportProto, _progressReports, _progressReportChances);
            var folders = SpawnIntel(FolderProto, _folders, _folderChances);
            var highs = SpawnIntel(TechnicalManualProto, _technicalManuals, _technicalManualChances);
            var disks = SpawnDataDisks(_disks);
            var dataTerminals = ActivateDataTerminalObjectives(_dataTerminals);
            var devices = SpawnIntel(ExperimentalDeviceProtos, _experimentalDevices, _experimentalDeviceChances, randomNumber: false);
            var safes = ActivateSafeObjectives(_safes);
            // SpawnIntel(ResearchPaperProto, _researchPapers, _researchPaperChances);
            // SpawnIntel(VialBoxProto, _vialBoxes, _vialBoxChances);

            AddFolderDetails(folders);
            AddDocumentLabels(highs, true);

            var mediums = new List<EntityUid>();
            mediums.AddRange(reports);
            mediums.AddRange(folders);

            tree.Comp.Tree.Documents.Total = lows.Count + mediums.Count + highs.Count;
            tree.Comp.Tree.UploadData.Total = disks.Count + dataTerminals.Count;
            tree.Comp.Tree.RetrieveItems.Total = tree.Comp.Tree.Documents.Total + disks.Count + devices.Count;
            tree.Comp.Tree.Miscellaneous.Total = safes.Count;
            Dirty(tree);

            var highTargets = new List<EntityUid>();
            highTargets.AddRange(highs);
            highTargets.AddRange(disks);
            highTargets.AddRange(safes);

            var extremeTargets = new List<EntityUid>();
            extremeTargets.AddRange(dataTerminals);
            extremeTargets.AddRange(devices);

            ConnectEachSourceToRandomTarget(lows, mediums);
            foreach (var medium in mediums)
            {
                AddRequires(medium, lows);
            }

            ConnectEachSourceToRandomTarget(mediums, highTargets);
            foreach (var high in highs)
            {
                AddRequires(high, mediums);
            }

            AddObjectiveClues(disks, mediums);
            AddObjectiveClues(safes, mediums);

            ConnectEachSourceToRandomTarget(highs, extremeTargets);
            AddObjectiveClues(dataTerminals, highs);
            AddObjectiveClues(devices, highs);

            UpdateTree(tree);
        }
        finally
        {
            _spawners.Clear();
        }
    }

    private void AddDocumentLabels(IEnumerable<EntityUid> documents, bool refreshNames = false)
    {
        foreach (var document in documents)
        {
            var details = EnsureComp<IntelClueDetailsComponent>(document);
            if (string.IsNullOrWhiteSpace(details.Label))
                details.Label = GenerateDocumentLabel();

            Dirty(document, details);
            if (refreshNames)
                _nameModifier.RefreshNameModifiers(document);
        }
    }

    private void AddFolderDetails(IEnumerable<EntityUid> folders)
    {
        foreach (var folder in folders)
        {
            var details = EnsureComp<IntelClueDetailsComponent>(folder);
            if (string.IsNullOrWhiteSpace(details.Label))
                details.Label = GenerateDocumentLabel();

            var color = _random.Pick(FolderColors);
            details.ColorName = color.Color;
            Dirty(folder, details);

            if (TryComp(folder, out RandomSpriteComponent? randomSprite))
            {
                randomSprite.Selected["base"] = (color.State, null);
                Dirty(folder, randomSprite);
            }

            _nameModifier.RefreshNameModifiers(folder);
        }
    }

    private List<EntityUid> SpawnDataDisks(int count)
    {
        return SpawnIntel(DiskProtos, count, _diskChances, onSpawn: SetupDataDisk);
    }

    private void SetupDataDisk(EntityUid diskId)
    {
        var disk = EnsureComp<IntelDataDiskComponent>(diskId);
        disk.EncryptionKey = GenerateAccessKey();
        disk.UploadProgress = 0;
        disk.Uploading = false;
        disk.Completed = false;
        disk.LastUser = null;
        Dirty(diskId, disk);

        var retrieve = EnsureComp<IntelRetrieveItemObjectiveComponent>(diskId);
        retrieve.State = IntelObjectiveState.Inactive;
        retrieve.Value = FixedPoint2.New(0.1);
        Dirty(diskId, retrieve);

        var clues = EnsureComp<IntelCluesComponent>(diskId);
        clues.Clue = "rmc-intel-clue-data-disk";
        clues.Category = "rmc-intel-data";
        clues.Clues = 2;
        SetInitialArea((diskId, clues));
        Dirty(diskId, clues);

        var details = EnsureComp<IntelClueDetailsComponent>(diskId);
        if (string.IsNullOrWhiteSpace(details.Label))
            details.Label = GenerateDataLabel();

        if (MetaData(diskId).EntityPrototype is { } proto &&
            DiskColors.TryGetValue(proto.ID, out var color))
        {
            details.ColorName = color;
        }

        Dirty(diskId, details);

        EnsureComp<IntelDetectorTrackedComponent>(diskId);
        _acid.SetCorrodible(diskId, false);
        _nameModifier.RefreshNameModifiers(diskId);
    }

    private List<EntityUid> ActivateDataTerminalObjectives(int count)
    {
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<IntelDataTerminalCandidateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (HasComp<IntelDataTerminalComponent>(uid))
                continue;

            if (_area.TryGetArea(uid, out var area, out _) &&
                area.Value.Comp.RetrieveItemObjective)
            {
                continue;
            }

            candidates.Add(uid);
        }

        _random.Shuffle(candidates);
        var terminals = new List<EntityUid>();
        foreach (var terminalId in candidates)
        {
            if (terminals.Count >= count)
                break;

            var terminal = EnsureComp<IntelDataTerminalComponent>(terminalId);
            terminal.Password = GenerateAccessKey();
            terminal.UploadProgress = 0;
            terminal.Uploading = false;
            terminal.Completed = false;
            terminal.LastUser = null;
            Dirty(terminalId, terminal);

            var details = EnsureComp<IntelClueDetailsComponent>(terminalId);
            if (string.IsNullOrWhiteSpace(details.Label))
                details.Label = GenerateDataLabel();

            Dirty(terminalId, details);
            _nameModifier.RefreshNameModifiers(terminalId);

            var clues = EnsureComp<IntelCluesComponent>(terminalId);
            clues.Clue = "rmc-intel-clue-data-terminal";
            clues.Category = "rmc-intel-data";
            clues.Clues = 2;
            SetInitialArea((terminalId, clues));

            EnsureComp<IntelDetectorTrackedComponent>(terminalId);
            terminals.Add(terminalId);
        }

        return terminals;
    }

    private List<EntityUid> ActivateSafeObjectives(int count)
    {
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<IntelSafeCandidateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (HasComp<IntelSafeObjectiveComponent>(uid))
                continue;

            if (_area.TryGetArea(uid, out var area, out _) &&
                area.Value.Comp.RetrieveItemObjective)
            {
                continue;
            }

            candidates.Add(uid);
        }

        _random.Shuffle(candidates);
        var safes = new List<EntityUid>();
        foreach (var safe in candidates)
        {
            if (safes.Count >= count)
                break;

            var objective = EnsureComp<IntelSafeObjectiveComponent>(safe);
            objective.Code = GenerateSafeCode();
            Dirty(safe, objective);

            var clues = EnsureComp<IntelCluesComponent>(safe);
            clues.Clue = "rmc-intel-clue-safe";
            clues.Category = "rmc-intel-misc";
            clues.Clues = 2;
            SetInitialArea((safe, clues));

            var details = EnsureComp<IntelClueDetailsComponent>(safe);
            if (string.IsNullOrWhiteSpace(details.Label))
                details.Label = GenerateDocumentLabel();

            Dirty(safe, details);
            _nameModifier.RefreshNameModifiers(safe);

            safes.Add(safe);
        }

        return safes;
    }

    private void AddObjectiveClues(List<EntityUid> targets, List<EntityUid> sources)
    {
        if (sources.Count == 0)
            return;

        foreach (var target in targets)
        {
            if (!TryComp(target, out IntelCluesComponent? clues) ||
                clues.Clues <= 0)
            {
                continue;
            }

            var existing = 0;
            if (TryComp(target, out IntelRequiresComponent? requires))
                existing = requires.Requires.Count;

            var left = clues.Clues - existing;
            for (var i = 0; i < left; i++)
            {
                _random.Shuffle(sources);
                var added = false;
                foreach (var source in sources)
                {
                    if (source == target || HasObjectiveLink(source, target))
                    {
                        continue;
                    }

                    ConnectObjectives(source, target);
                    added = true;
                    break;
                }

                if (!added)
                    break;
            }
        }
    }

    private void ConnectEachSourceToRandomTarget(List<EntityUid> sources, List<EntityUid> targets)
    {
        if (targets.Count == 0)
            return;

        foreach (var source in sources)
        {
            _random.Shuffle(targets);
            foreach (var target in targets)
            {
                if (source == target ||
                    HasObjectiveLink(source, target) ||
                    HasEnoughObjectiveLinks(target))
                {
                    continue;
                }

                ConnectObjectives(source, target);
                break;
            }
        }
    }

    private bool HasObjectiveLink(EntityUid unlocksId, EntityUid requiresId)
    {
        return TryComp(requiresId, out IntelRequiresComponent? requires) &&
               requires.Requires.Contains(unlocksId);
    }

    private bool HasEnoughObjectiveLinks(EntityUid requiresId)
    {
        if (!TryComp(requiresId, out IntelRequiresComponent? requires))
            return false;

        if (TryComp(requiresId, out IntelCluesComponent? clues) &&
            clues.Clues > 0)
        {
            return requires.Requires.Count >= clues.Clues;
        }

        return requires.Requires.Count >= requires.RequiresCount;
    }

    public void RestoreColonyCommunications()
    {
        if (_net.IsClient)
            return;

        var tree = EnsureTechTree();
        if (tree.Comp.Tree.ColonyCommunications)
            return;

        tree.Comp.Tree.ColonyCommunications = true;
        AddPoints(tree, tree.Comp.ColonyCommunicationsPoints);
    }

    private void ConnectObjectives(EntityUid unlocksId, EntityUid requiresId)
    {
        var unlocks = EnsureComp<IntelUnlocksComponent>(unlocksId);
        unlocks.Unlocks.Add(requiresId);
        Dirty(unlocksId, unlocks);

        var requires = EnsureComp<IntelRequiresComponent>(requiresId);
        requires.Requires.Add(unlocksId);
        Dirty(requiresId, requires);

        DeactivateIntel(requiresId);
    }

    private void AddRequires(Entity<IntelRequiresComponent?> requires, List<EntityUid> candidates)
    {
        requires.Comp ??= EnsureComp<IntelRequiresComponent>(requires);
        if (requires.Comp.RequiresCount <= requires.Comp.Requires.Count)
            return;

        var left = requires.Comp.RequiresCount - requires.Comp.Requires.Count;
        for (var i = 0; i < left; i++)
        {
            _random.Shuffle(candidates);
            foreach (var candidate in candidates)
            {
                if (requires.Comp.Requires.Contains(candidate))
                    continue;

                ConnectObjectives(candidate, requires);

                if (requires.Comp.RequiresCount <= requires.Comp.Requires.Count)
                    break;
            }

            if (requires.Comp.RequiresCount <= requires.Comp.Requires.Count)
                break;
        }
    }

    private void DeactivateIntel(EntityUid ent)
    {
        if (TryComp(ent, out IntelReadObjectiveComponent? read))
        {
            read.State = IntelObjectiveState.Inactive;
            Dirty(ent, read);
        }

        if (TryComp(ent, out IntelRetrieveItemObjectiveComponent? retrieve))
        {
            retrieve.State = IntelObjectiveState.Inactive;
            Dirty(ent, retrieve);
        }
    }

    private void ActivateIntel(EntityUid activatedBy, EntityUid toActivate)
    {
        if (TryComp(toActivate, out IntelReadObjectiveComponent? read) &&
            read.State == IntelObjectiveState.Inactive)
        {
            read.State = IntelObjectiveState.Active;
            Dirty(toActivate, read);
        }

        if (TryComp(toActivate, out IntelRetrieveItemObjectiveComponent? retrieve) &&
            retrieve.State == IntelObjectiveState.Inactive &&
            (!TryComp(toActivate, out IntelDataDiskComponent? disk) || disk.Completed))
        {
            retrieve.State = IntelObjectiveState.Active;
            Dirty(toActivate, retrieve);
        }

        var label = GetClueLabel(toActivate);
        if (!string.IsNullOrWhiteSpace(label) &&
            label != Loc.GetString("rmc-intel-clue-label-unmarked"))
        {
            var unlocked = EnsureComp<IntelHasUnlockedComponent>(activatedBy);
            unlocked.Unlocked.Add(label);
            Dirty(activatedBy, unlocked);

            _nameModifier.RefreshNameModifiers(activatedBy);
        }
    }

    public bool TryUsePoints(FixedPoint2 points)
    {
        var tree = EnsureTechTree();
        if (points > tree.Comp.Tree.Points)
            return false;

        tree.Comp.Tree.Points -= points;
        Dirty(tree);
        UpdateTree(tree);
        return true;
    }

    public void AddPoints(Entity<IntelTechTreeComponent> tree, FixedPoint2 points)
    {
        tree.Comp.Tree.Points += points;
        tree.Comp.Tree.TotalEarned += points;
        Dirty(tree);
        UpdateTree(tree);
    }

    public void AddPoints(FixedPoint2 points)
    {
        if (TryGetTechTree(out var tree))
            AddPoints(tree.Value, points);
    }

    public void UpdateTree(Entity<IntelTechTreeComponent> tree)
    {
        var query = EntityQueryEnumerator<TechControlConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            console.Tree = tree.Comp.Tree;
            Dirty(uid, console);
        }

        var viewQuery = EntityQueryEnumerator<ViewIntelObjectivesComponent>();
        while (viewQuery.MoveNext(out var uid, out var view))
        {
            view.Tree = tree.Comp.Tree;
            Dirty(uid, view);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (TryGetTechTree(out var tree) &&
            tree.Value.Comp.DoAnnouncements &&
            time >= tree.Value.Comp.LastAnnounceAt)
        {
            tree.Value.Comp.LastAnnounceAt = time + _announceEvery;
            Dirty(tree.Value);

            var ares = _aresCore.EnsureMarineARES();
            var points = tree.Value.Comp.Tree.Points;
            var last = tree.Value.Comp.LastAnnouncePoints;
            tree.Value.Comp.LastAnnouncePoints = points;
            Dirty(tree.Value);

            var change = points - last;
            foreach (var channel in tree.Value.Comp.AnnounceIn)
            {
                var announcement = change > FixedPoint2.Zero
                    ? Loc.GetString("rmc-intel-announcement-gain", ("points", points), ("change", change))
                    : Loc.GetString("rmc-intel-announcement", ("points", points));

                _marineAnnounce.AnnounceRadio(ares, announcement, channel);
            }
        }

        var terminalQuery = EntityQueryEnumerator<IntelDataTerminalComponent>();
        while (terminalQuery.MoveNext(out var uid, out var terminal))
        {
            if (!terminal.Uploading || terminal.Completed)
                continue;

            if (terminal.LastUser is not { Valid: true } terminalUser ||
                !CanUploadData((uid, terminal), terminalUser, out _))
            {
                terminal.Uploading = false;
                Dirty(uid, terminal);
                continue;
            }

            terminal.UploadProgress += frameTime;
            if (terminal.UploadProgress < terminal.UploadTime)
            {
                Dirty(uid, terminal);
                continue;
            }

            terminal.Uploading = false;
            terminal.Completed = true;
            Dirty(uid, terminal);
            CompleteUpload(uid, terminal.Value);
            _popup.PopupEntity(Loc.GetString("rmc-intel-data-terminal-finished"), uid, terminalUser);
        }

        var readerQuery = EntityQueryEnumerator<IntelDiskReaderComponent>();
        while (readerQuery.MoveNext(out var uid, out var reader))
        {
            if (!TryGetReaderDisk((uid, reader), out var disk))
                continue;

            if (!_power.IsPowered(uid))
            {
                var diskUser = disk.Value.Comp.LastUser ?? reader.LastUser;
                EjectDisk((uid, reader), disk.Value, diskUser);
                if (diskUser is { Valid: true } diskUserId)
                    _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-power-lost"), uid, diskUserId, PopupType.MediumCaution);

                continue;
            }

            if (!disk.Value.Comp.Uploading || disk.Value.Comp.Completed)
                continue;

            disk.Value.Comp.UploadProgress += frameTime;
            if (disk.Value.Comp.UploadProgress < disk.Value.Comp.UploadTime)
            {
                Dirty(disk.Value);
                continue;
            }

            disk.Value.Comp.Uploading = false;
            disk.Value.Comp.Completed = true;
            Dirty(disk.Value);
            _nameModifier.RefreshNameModifiers(disk.Value.Owner);
            CompleteUpload(disk.Value.Owner, disk.Value.Comp.UploadValue);

            if (TryComp(disk.Value.Owner, out IntelRetrieveItemObjectiveComponent? retrieve) &&
                retrieve.State == IntelObjectiveState.Inactive)
            {
                retrieve.State = IntelObjectiveState.Active;
                Dirty(disk.Value.Owner, retrieve);
                EnsureComp<ActiveIntelPositionComponent>(disk.Value.Owner);
            }

            var completedUser = disk.Value.Comp.LastUser ?? reader.LastUser;
            EjectDisk((uid, reader), disk.Value, completedUser);
            if (completedUser is { Valid: true } completedUserId)
                _popup.PopupEntity(Loc.GetString("rmc-intel-disk-reader-finished"), uid, completedUserId);
        }

        if (_activePositionIntels.Count > 0)
        {
            while (_activePositionIntels.TryDequeue(out var intel))
            {
                if (_timing.CurTime >= time + _maxProcessTime)
                    return;

                if (TerminatingOrDeleted(intel))
                    continue;

                if (intel.Comp.State == IntelObjectiveState.Complete)
                    continue;

                if (_readObjectiveQuery.TryComp(intel, out var read) &&
                    read.State != IntelObjectiveState.Complete)
                {
                    continue;
                }

                if (_area.TryGetArea(intel.Owner, out var area, out _) &&
                    area.Value.Comp.RetrieveItemObjective)
                {
                    intel.Comp.State = IntelObjectiveState.Complete;
                    Dirty(intel);

                    tree ??= EnsureTechTree();
                    tree.Value.Comp.Tree.RetrieveItems.Current++;
                    RemoveClue(tree.Value, intel);
                    AddPoints(tree.Value, intel.Comp.Value);

                    RemComp<ActiveIntelPositionComponent>(intel);
                }
            }
        }

        if (_activeSurvivorIntels.Count > 0)
        {
            while (_activeSurvivorIntels.TryDequeue(out var intel))
            {
                if (_timing.CurTime >= time + _maxProcessTime)
                    return;

                if (TerminatingOrDeleted(intel))
                    continue;

                if (_mobState.IsDead(intel))
                    continue;

                if (_area.TryGetArea(intel.Owner, out var area, out _) &&
                    HasComp<IntelRescueSurvivorAreaComponent>(area))
                {
                    tree ??= EnsureTechTree();
                    tree.Value.Comp.Tree.RescueSurvivors++;
                    AddPoints(tree.Value, intel.Comp.Value);

                    RemComp<IntelRescueSurvivorObjectiveComponent>(intel);
                }
            }
        }

        if (_activeCorpseIntels.Count > 0)
        {
            while (_activeCorpseIntels.TryDequeue(out var intel))
            {
                if (_timing.CurTime >= time + _maxProcessTime)
                    return;

                if (TerminatingOrDeleted(intel))
                    continue;

                if (!_mobState.IsDead(intel))
                    continue;

                if (_area.TryGetArea(intel.Owner, out var area, out _) &&
                    HasComp<IntelRecoverCorpsesAreaComponent>(area))
                {
                    tree ??= EnsureTechTree();
                    tree.Value.Comp.Tree.RecoverCorpses++;
                    Dirty(tree.Value);

                    RemComp<ActiveIntelCorpseComponent>(intel);

                    if (!HasComp<XenoComponent>(intel))
                    {
                        if (tree.Value.Comp.HumanoidCorpses >= _intelHumanoidsCorpsesMax)
                            continue;

                        tree.Value.Comp.HumanoidCorpses++;
                    }

                    AddPoints(tree.Value, intel.Comp.Value);
                }
            }
        }

        var activeIntelQuery = EntityQueryEnumerator<ActiveIntelPositionComponent, IntelRetrieveItemObjectiveComponent>();
        while (activeIntelQuery.MoveNext(out var uid, out _, out var retrieve))
        {
            if (retrieve.State == IntelObjectiveState.Active)
                _activePositionIntels.Enqueue((uid, retrieve));
        }

        var survivorQuery = EntityQueryEnumerator<IntelRescueSurvivorObjectiveComponent>();
        while (survivorQuery.MoveNext(out var uid, out var comp))
        {
            _activeSurvivorIntels.Enqueue((uid, comp));
        }

        var corpseQuery = EntityQueryEnumerator<ActiveIntelCorpseComponent, IntelRecoverCorpseObjectiveComponent>();
        while (corpseQuery.MoveNext(out var uid, out _, out var comp))
        {
            _activeCorpseIntels.Enqueue((uid, comp));
        }

        if (tree != null && !tree.Value.Comp.Tree.ColonyPower)
        {
            var watts = 0;
            var generatorQuery = EntityQueryEnumerator<IntelPowerObjectiveComponent, RMCFusionReactorComponent>();
            while (generatorQuery.MoveNext(out _, out var generator))
            {
                if (generator.State != RMCFusionReactorState.Working)
                    continue;

                watts += generator.Watts;
            }

            if (watts >= _powerObjectiveWattsRequired)
            {
                tree.Value.Comp.Tree.ColonyPower = true;
                AddPoints(tree.Value, tree.Value.Comp.PowerPoints);
            }
        }
    }
}
