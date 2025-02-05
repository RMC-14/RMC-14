using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<IntelSpawnerType, List<Entity<IntelSpawnerComponent>>> _spawners = new();
    private readonly Queue<Entity<IntelRetrieveItemObjectiveComponent>> _activePositionIntels = new();
    private readonly HashSet<Entity<IntelContainerComponent>> _nearby = new();

    private static readonly EntProtoId<IntelTechTreeComponent> TechTreeProto = "RMCIntelTechTree";

    private static readonly EntProtoId PaperScrapProto = "RMCIntelPaperScrap";
    private static readonly EntProtoId ProgressReportProto = "RMCIntelProgressReport";
    private static readonly EntProtoId FolderProto = "RMCIntelFolder";
    private static readonly EntProtoId TechnicalManualProto = "RMCIntelTechnicalManual";
    private static readonly EntProtoId DiskProto = "RMCIntelDisk";
    private static readonly EntProtoId ExperimentalDevicesProto = "RMCIntelExperimentalDevice";
    private static readonly EntProtoId ResearchPaperProto = "RMCIntelExperimentalDevice";
    private static readonly EntProtoId VialBoxProto = "RMCIntelVialBox";

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
    private int _experimentalDevices;
    private int _researchPapers;
    private int _vialBoxes;
    private TimeSpan _maxProcessTime;

    private EntityQuery<IntelReadObjectiveComponent> _readObjectiveQuery;

    public override void Initialize()
    {
        _readObjectiveQuery = GetEntityQuery<IntelReadObjectiveComponent>();

        SubscribeLocalEvent<IntelNumberComponent, RefreshNameModifiersEvent>(OnNumberRefreshNameModifiers);

        SubscribeLocalEvent<IntelUnlocksComponent, ComponentRemove>(OnUnlocksRemove);
        SubscribeLocalEvent<IntelUnlocksComponent, EntityTerminatingEvent>(OnUnlocksRemove);

        SubscribeLocalEvent<IntelRequiresComponent, ComponentRemove>(OnRequiresRemove);
        SubscribeLocalEvent<IntelRequiresComponent, EntityTerminatingEvent>(OnRequiresRemove);

        SubscribeLocalEvent<IntelReadObjectiveComponent, UseInHandEvent>(OnReadUseInHand);
        SubscribeLocalEvent<IntelReadObjectiveComponent, IntelReadDoAfterEvent>(OnReadDoAfter);

        SubscribeLocalEvent<IntelRetrieveItemObjectiveComponent, MapInitEvent>(OnRetrieveMapInit);

        SubscribeLocalEvent<ViewIntelObjectivesComponent, MapInitEvent>(OnViewIntelObjectivesMapInit);
        SubscribeLocalEvent<ViewIntelObjectivesComponent, ViewIntelObjectivesActionEvent>(OnViewIntelObjectivesAction);

        SubscribeLocalEvent<IntelHasUnlockedComponent, RefreshNameModifiersEvent>(OnHasUnlockedRefreshName);

        Subs.CVar(_config, RMCCVars.RMCIntelPaperScraps, v => _paperScraps = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelProgressReports, v => _progressReports = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelFolders, v => _folders = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelTechnicalManuals, v => _technicalManuals = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelDisks, v => _disks = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelExperimentalDevices, v => _experimentalDevices = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelResearchPapers, v => _researchPapers = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelVialBoxes, v => _vialBoxes = v, true);
        Subs.CVar(_config, RMCCVars.RMCIntelMaxProcessTimeMilliseconds, v => _maxProcessTime = TimeSpan.FromMilliseconds(v), true);
    }

    private void OnNumberRefreshNameModifiers(Entity<IntelNumberComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-intel-suffix", extraArgs: ("number", ent.Comp.Number));
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
        var delay = ent.Comp.Delay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var ev = new IntelReadDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent);
        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient($"You start reading the {Name(ent)}", ent, user);
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
            _popup.PopupClient("You get distracted and lose your train of thought, you'll have to start over reading this.", ent, user);
            return;
        }

        if (ent.Comp.State == IntelObjectiveState.Inactive)
        {
            _popup.PopupClient("You don't notice anything useful. You probably need to find its instructions on a paper scrap", ent, user);
            return;
        }

        _popup.PopupClient($"You finish reading the {Name(ent)}", ent, user);
        if (ent.Comp.State == IntelObjectiveState.Complete)
            return;

        ent.Comp.State = IntelObjectiveState.Complete;
        Dirty(ent);

        if (_net.IsClient)
            return;

        var tree = EnsureTechTree();
        tree.Comp.Tree.Points += ent.Comp.Value;
        tree.Comp.Tree.TotalEarned += ent.Comp.Value;
        Dirty(tree);

        if (TryComp(ent, out IntelUnlocksComponent? unlocks))
        {
            foreach (var unlock in unlocks.Unlocks)
            {
                ActivateIntel(ent, unlock);
            }
        }

        UpdateTree();
    }

    private void OnRetrieveMapInit(Entity<IntelRetrieveItemObjectiveComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.State != IntelObjectiveState.Active)
            return;

        EnsureComp<ActiveIntelPositionComponent>(ent);
    }

    private void OnViewIntelObjectivesMapInit(Entity<ViewIntelObjectivesComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnViewIntelObjectivesAction(Entity<ViewIntelObjectivesComponent> ent, ref ViewIntelObjectivesActionEvent args)
    {
        if (_net.IsServer)
        {
            var tree = EnsureTechTree().Comp.Tree;
            ent.Comp.Tree = tree;
            Dirty(ent);
        }

        _ui.OpenUi(ent.Owner, ViewIntelObjectivesUI.Key, ent);
    }

    private void OnHasUnlockedRefreshName(Entity<IntelHasUnlockedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-intel-unlocked", extraArgs: ("unlocked", string.Join(", ", ent.Comp.Unlocked)));
    }

    private List<EntityUid> SpawnIntel(EntProtoId proto, int count, Dictionary<IntelSpawnerType, float> chances)
    {
        var items = new List<EntityUid>();
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
            var intel = Spawn(proto, coords);
            items.Add(intel);

            EnsureComp<ActiveIntelPositionComponent>(intel);

            var number = EnsureComp<IntelNumberComponent>(intel);
            number.Number = _random.Next(100, 1000);
            Dirty(intel, number);
            _nameModifier.RefreshNameModifiers(intel);

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
        var techTreeQuery = EntityQueryEnumerator<IntelTechTreeComponent>();
        while (techTreeQuery.MoveNext(out var uid, out var comp))
        {
            return (uid, comp);
        }

        var ent = Spawn(TechTreeProto);
        var tree = EnsureComp<IntelTechTreeComponent>(ent);

        foreach (var tier in tree.Tree.Options)
        {
            for (var i = 0; i < tier.Count; i++)
            {
                var option = tier[i];
                if (option.CurrentCost == default)
                    tier[i] = option with { CurrentCost = option.Cost };
            }
        }

        return (ent, tree);
    }

    private void RunSpawners()
    {
        try
        {
            var spawnerQuery = EntityQueryEnumerator<IntelSpawnerComponent>();
            while (spawnerQuery.MoveNext(out var uid, out var comp))
            {
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
            var mediums = SpawnIntel(ProgressReportProto, _progressReports, _progressReportChances);
            mediums.AddRange(SpawnIntel(FolderProto, _folders, _folderChances));
            var highs = SpawnIntel(TechnicalManualProto, _technicalManuals, _technicalManualChances);
            // SpawnIntel(DiskProto, _disks, _diskChances);
            // SpawnIntel(ExperimentalDevicesProto, _experimentalDevices, _experimentalDeviceChances);
            // SpawnIntel(ResearchPaperProto, _researchPapers, _researchPaperChances);
            // SpawnIntel(VialBoxProto, _vialBoxes, _vialBoxChances);

            tree.Comp.Tree.Documents.Total = _paperScraps + _progressReports + _folders + _technicalManuals;
            tree.Comp.Tree.UploadData.Total = _disks;
            tree.Comp.Tree.RetrieveItems.Total = tree.Comp.Tree.Documents.Total + tree.Comp.Tree.UploadData.Total;

            if (mediums.Count > 0)
            {
                foreach (var low in lows)
                {
                    var medium = _random.Pick(mediums);
                    ConnectObjectives(low, medium);
                }
            }

            if (highs.Count > 0)
            {
                foreach (var medium in mediums)
                {
                    AddRequires(medium, lows);
                    var high = _random.Pick(highs);
                    ConnectObjectives(medium, high);
                }
            }

            if (mediums.Count > 0)
            {
                foreach (var high in highs)
                {
                    AddRequires(high, mediums);
                }
            }
        }
        finally
        {
            _spawners.Clear();
        }
    }

    public void RestoreColonyCommunications()
    {
        if (_net.IsClient)
            return;

        var tree = EnsureTechTree();
        if (tree.Comp.Tree.ColonyCommunications)
            return;

        tree.Comp.Tree.ColonyCommunications = true;

        var points = tree.Comp.ColonyCommunicationsPoints;
        tree.Comp.Tree.Points += points;
        tree.Comp.Tree.TotalEarned += points;
        Dirty(tree);

        UpdateTree();
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
        if (!Resolve(requires, ref requires.Comp, false))
            return;

        if (requires.Comp.RequiresCount <= requires.Comp.Requires.Count)
            return;

        while (requires.Comp.RequiresCount < requires.Comp.Requires.Count &&
               requires.Comp.RequiresCount <= candidates.Count)
        {
            var low = _random.Pick(candidates);
            if (requires.Comp.Requires.Contains(low))
            {
                if (candidates.Count < requires.Comp.RequiresCount)
                    break;
            }

            ConnectObjectives(low, requires);
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
            retrieve.State == IntelObjectiveState.Inactive)
        {
            retrieve.State = IntelObjectiveState.Active;
            Dirty(toActivate, retrieve);
        }

        if (TryComp(toActivate, out IntelNumberComponent? number))
        {
            var unlocked = EnsureComp<IntelHasUnlockedComponent>(activatedBy);
            unlocked.Unlocked.Add(number.Number);
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
        UpdateTree();
        return true;
    }

    public void UpdateTree()
    {
        var tree = EnsureTechTree();
        var query = EntityQueryEnumerator<TechControlConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            console.Tree = tree.Comp.Tree;
            Dirty(uid, console);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        RunSpawners();

        if (_activePositionIntels.Count > 0)
        {
            var time = _timing.CurTime;
            while (_activePositionIntels.TryDequeue(out var activeIntel))
            {
                if (TerminatingOrDeleted(activeIntel))
                    continue;

                if (activeIntel.Comp.State == IntelObjectiveState.Complete)
                    continue;

                if (_readObjectiveQuery.TryComp(activeIntel, out var read) &&
                    read.State != IntelObjectiveState.Complete)
                {
                    if (_timing.CurTime >= time + _maxProcessTime)
                        break;

                    continue;
                }

                if (_area.TryGetArea(activeIntel.Owner, out var area, out _) &&
                    area.Value.Comp.RetrieveItemObjective)
                {
                    activeIntel.Comp.State = IntelObjectiveState.Complete;
                    Dirty(activeIntel);

                    var tree = EnsureTechTree();
                    tree.Comp.Tree.RetrieveItems.Current++;
                    tree.Comp.Tree.Points += activeIntel.Comp.Value;
                    tree.Comp.Tree.TotalEarned += activeIntel.Comp.Value;
                    Dirty(tree);

                    RemComp<ActiveIntelPositionComponent>(activeIntel);
                    UpdateTree();
                }

                if (_timing.CurTime >= time + _maxProcessTime)
                    break;
            }

            return;
        }

        var activeIntelQuery = EntityQueryEnumerator<ActiveIntelPositionComponent, IntelRetrieveItemObjectiveComponent>();
        while (activeIntelQuery.MoveNext(out var uid, out _, out var retrieve))
        {
            if (retrieve.State == IntelObjectiveState.Active)
                _activePositionIntels.Enqueue((uid, retrieve));
        }
    }
}
