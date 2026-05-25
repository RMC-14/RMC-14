using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

public sealed class HiveBoonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCGameTickerSystem _rmcGameTicker = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

    private static readonly EntProtoId<HiveBoonDefinitionComponent> KingBoonId = "RMCHiveBoonKing";
    private static readonly EntProtoId<HiveKingCocoonComponent> KingCocoonId = "RMCHiveCocoonKing";
    private static readonly EntProtoId TransmuteActionId = "ActionXenoTransmute";
    private static readonly TimeSpan FortifiedStructuresUpdateEvery = TimeSpan.FromSeconds(1);

    public ImmutableArray<(EntityPrototype Prototype, HiveBoonDefinitionComponent Component)> Boons
    {
        get;
        private set;
    } = ImmutableArray<(EntityPrototype Prototype, HiveBoonDefinitionComponent Component)>.Empty;

    private int _aliveMarineRequirement;
    private TimeSpan _royalResinEvery;
    public TimeSpan CommunicationTowerXenoTakeoverTime { get; private set; }
    private TimeSpan _kingVoteCandidateTimeRequired;
    private TimeSpan _kingFirstWarningTime;
    private TimeSpan _kingVoteStartTime;
    private TimeSpan _kingVoteAskCandidatesTime;
    private TimeSpan _kingVoteStartHatchingTime;
    private TimeSpan _earlyEvoBoostBefore;
    private TimeSpan _nextFortifiedStructuresUpdate;

    private EntityQuery<ExcludedFromKingVoteComponent> _excludedFromKingVoteQuery;

    private readonly HashSet<ProtoId<PlayTimeTrackerPrototype>> _xenoJobs = new();
    private readonly HashSet<EntityUid> _fortifiedStructureHives = new();

    public override void Initialize()
    {
        _excludedFromKingVoteQuery = GetEntityQuery<ExcludedFromKingVoteComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<HiveBoonActivateFireResistanceEvent>(OnActivateFireResistance);
        SubscribeLocalEvent<HiveBoonActivateLarvaSurgeEvent>(OnActivateLarvaSurge);
        SubscribeLocalEvent<HiveBoonActivateKingEvent>(OnActivateKing);
        SubscribeLocalEvent<HiveBoonActivateEvolutionEvent>(OnActivateEvolution);
        SubscribeLocalEvent<HiveBoonActivateAdaptabilityEvent>(OnActivateAdaptability);
        SubscribeLocalEvent<HiveBoonActivateAggressionEvent>(OnActivateAggression);
        SubscribeLocalEvent<HiveBoonActivateFortificationEvent>(OnActivateFortification);

        SubscribeLocalEvent<XenoComponent, RMCGetFireImmunityEvent>(OnGetTileFireImmunity);
        SubscribeLocalEvent<XenoComponent, GetIgnitionImmunityEvent>(OnGetTileFireIgnitionImmunity);
        SubscribeLocalEvent<XenoComponent, HiveKingVoteDialogEvent>(OnKingVote);
        SubscribeLocalEvent<HiveBoonDefinitionComponent, EntityTerminatingEvent>(OnBoonTerminating);

        SubscribeLocalEvent<HiveClusterComponent, ExaminedEvent>(OnClusterExamined);
        SubscribeLocalEvent<HiveClusterComponent, AfterEntityWeedingEvent>(OnClusterAfterWeeding);
        SubscribeLocalEvent<HiveClusterComponent, DamageChangedEvent>(OnClusterDamageChanged);

        SubscribeLocalEvent<HivePylonComponent, ExaminedEvent>(OnPylonExamined);
        SubscribeLocalEvent<HivePylonComponent, EntityTerminatingEvent>(OnPylonTerminating);

        SubscribeLocalEvent<HiveBoonFortifiableWallComponent, DamageChangedEvent>(OnFortifiableWallDamageChanged);

        SubscribeLocalEvent<CommunicationsTowerComponent, CommunicationsTowerStateChangedEvent>(OnTowerBreak);
        SubscribeLocalEvent<CommunicationsTowerComponent, RMCRepairableTargetAttemptEvent>(OnTowerRepairAttempt);

        SubscribeLocalEvent<HiveKingCocoonComponent, EntityTerminatingEvent>(OnCocoonTerminating);

        Subs.CVar(_config,
            RMCCVars.RMCBoonsLiveMarineRequirement,
            v => _aliveMarineRequirement = v,
            true);

        Subs.CVar(_config,
            RMCCVars.RMCRoyalResinEveryMinutes,
            v => _royalResinEvery = TimeSpan.FromMinutes(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCCommunicationTowerXenoTakeoverMinutes,
            v => CommunicationTowerXenoTakeoverTime = TimeSpan.FromMinutes(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCKingVoteCandidateTimeRequirementHours,
            v => _kingVoteCandidateTimeRequired = TimeSpan.FromHours(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCKingHatchingFirstWarningMinutes,
            v => _kingFirstWarningTime = TimeSpan.FromMinutes(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCKingVoteStartTimeSeconds,
            v => _kingVoteStartTime = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCKingVoteAskCandidatesTimeSeconds,
            v => _kingVoteAskCandidatesTime = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCKingVoteStartHatchingTimeSeconds,
            v => _kingVoteStartHatchingTime = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCXenoEarlyEvoPointBoostBeforeMinutes,
            v => _earlyEvoBoostBefore = TimeSpan.FromMinutes(v),
            true);

        ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPrototypes();
    }

    private void OnActivateFireResistance(HiveBoonActivateFireResistanceEvent ev)
    {
        EnsureComp<HiveBoonFireImmunityComponent>(ev.Boon);
        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateLarvaSurge(HiveBoonActivateLarvaSurgeEvent ev)
    {
        _hive.IncreaseBurrowedLarva(ev.Hive, 5);
        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateEvolution(HiveBoonActivateEvolutionEvent ev)
    {
        var evolution = EnsureComp<HiveBoonEvolutionComponent>(ev.Boon);
        evolution.Multiplier = ev.Multiplier;
        evolution.BypassOvipositor = ev.BypassOvipositor;
        evolution.FrozenEarlyEvolutionBoost = _earlyEvoBoostBefore > _gameTicker.RoundDuration();

        var bonus = FixedPoint2.Zero;
        var bonuses = EntityQueryEnumerator<EvolutionBonusComponent>();
        while (bonuses.MoveNext(out var comp))
        {
            bonus += comp.Amount;
        }

        evolution.FrozenBonus = bonus;
        evolution.HasFrozenOverride = false;
        evolution.FrozenOverride = FixedPoint2.Zero;

        var overrides = EntityQueryEnumerator<EvolutionOverrideComponent>();
        while (overrides.MoveNext(out var comp))
        {
            evolution.HasFrozenOverride = true;
            evolution.FrozenOverride = comp.Amount;
        }

        Dirty(ev.Boon, evolution);

        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateAdaptability(HiveBoonActivateAdaptabilityEvent ev)
    {
        var xenos = EntityQueryEnumerator<XenoComponent>();
        while (xenos.MoveNext(out var uid, out var xeno))
        {
            if (!_hive.FromSameHive(uid, ev.Boon) ||
                !CanReceiveTransmuteAction((uid, xeno)))
            {
                continue;
            }

            if (_action.AddAction(uid, TransmuteActionId) == null)
                continue;

            EnsureComp<HiveBoonAdaptabilityActionComponent>(uid);
        }

        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateAggression(HiveBoonActivateAggressionEvent ev)
    {
        var aggression = EnsureComp<HiveBoonAggressionComponent>(ev.Boon);
        aggression.Damage = ev.Damage;
        Dirty(ev.Boon, aggression);

        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateFortification(HiveBoonActivateFortificationEvent ev)
    {
        EnsureComp<HiveBoonFortificationComponent>(ev.Boon);
        _nextFortifiedStructuresUpdate = TimeSpan.Zero;
        QueueDamagedFortifiedStructures(ev.Boon);
        AnnounceBoonActivation(ev.Boon);
    }

    private void OnActivateKing(HiveBoonActivateKingEvent ev)
    {
        if (ev.Core is not { } core)
            return;

        var cocoon = SpawnAtPosition(KingCocoonId, core.ToCoordinates());
        _hive.SetHive(cocoon, ev.Hive);
        ApplyKingPylonObProtection(cocoon);

        var areaName = _area.GetAreaName(core);
        _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-marine", ("area", areaName)));
        _xenoAnnounce.AnnounceSameHiveDefaultSound(core, Loc.GetString("rmc-boon-king-announcement-xenos", ("area", areaName)));
        AnnounceBoonActivation(ev.Boon);
    }

    private void OnGetTileFireImmunity(Entity<XenoComponent> xeno, ref RMCGetFireImmunityEvent ev)
    {
        var query = EntityQueryEnumerator<HiveBoonFireImmunityComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_hive.FromSameHive(uid, xeno.Owner))
                continue;

            ev.Ignite = false;
            ev.Immune = true;
            return;
        }
    }

    private void OnGetTileFireIgnitionImmunity(Entity<XenoComponent> xeno, ref GetIgnitionImmunityEvent args)
    {
        var query = EntityQueryEnumerator<HiveBoonFireImmunityComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_hive.FromSameHive(uid, xeno.Owner))
                args.Ignite = false;
        }
    }

    private void OnKingVote(Entity<XenoComponent> ent, ref HiveKingVoteDialogEvent args)
    {
        if (GetEntity(args.Cocoon) is not { Valid: true } cocoon ||
            GetEntity(args.Voted) is not { Valid: true } votedId)
        {
            return;
        }

        GetKingVotingData(ent.Owner, cocoon, out _, out var canVote);
        if (!canVote)
            return;

        GetKingVotingData(votedId, cocoon, out var canBeKing, out _);
        if (!canBeKing)
            return;

        if (!TryComp(votedId, out ActorComponent? actor))
            return;

        var vote = EnsureVote(cocoon);
        var votedUserId = actor.PlayerSession.UserId;
        vote.Comp.Votes[votedUserId] = vote.Comp.Votes.GetValueOrDefault(votedUserId) + 1;
        Dirty(vote);
    }

    private void OnPylonExamined(Entity<HivePylonComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(HivePylonComponent)))
        {
            var msg = Loc.GetString("rmc-boon-pylon-examine", ("minutes", (int)_royalResinEvery.TotalMinutes));
            args.PushMarkup(msg);
        }
    }

    private void OnPylonTerminating(Entity<HivePylonComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp(ent, out TransformComponent? xform) ||
            TerminatingOrDeleted(xform.MapUid) ||
            HasComp<HiveConstructionSuppressAnnouncementsComponent>(ent))
        {
            return;
        }

        var area = _area.GetAreaName(ent);
        _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-pylon-destroyed-announcement-marine", ("area", area)));
        _xenoAnnounce.AnnounceSameHiveDefaultSound(ent.Owner, Loc.GetString("rmc-boon-pylon-destroyed-announcement-xeno", ("area", area)));

        if (ent.Comp.Tower is { } tower)
        {
            _appearance.SetData(tower, WeededEntityLayers.Layer, false);
            if (TryComp(tower, out CommunicationsTowerComponent? towerComp))
            {
                towerComp.XenoControlled = false;
                Dirty(tower, towerComp);
            }
        }
    }

    private void OnClusterExamined(Entity<HiveClusterComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(HivePylonComponent)))
        {
            var msg = Loc.GetString("rmc-boon-cluster-examine", ("minutes", (int)CommunicationTowerXenoTakeoverTime.TotalMinutes));
            args.PushMarkup(msg);
        }
    }

    private void OnClusterAfterWeeding(Entity<HiveClusterComponent> ent, ref AfterEntityWeedingEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            !TryComp(args.CoveredEntity, out CommunicationsTowerComponent? tower))
        {
            return;
        }

        ReplaceCluster(ent, (args.CoveredEntity, tower));
    }

    private void OnClusterDamageChanged(Entity<HiveClusterComponent> ent, ref DamageChangedEvent args)
    {
        QueueFortificationRepair(ent.Owner, args);
    }

    private void OnFortifiableWallDamageChanged(Entity<HiveBoonFortifiableWallComponent> ent, ref DamageChangedEvent args)
    {
        QueueFortificationRepair(ent.Owner, args);
    }

    private void OnTowerBreak(Entity<CommunicationsTowerComponent> ent, ref CommunicationsTowerStateChangedEvent args)
    {
        if (ent.Comp.State != CommunicationsTowerState.Broken)
            return;

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(ent);
        while (anchored.MoveNext(out var uid))
        {
            if (TryComp(uid, out XenoWeedsComponent? weeds) &&
                TryComp(weeds.Source, out HiveClusterComponent? cluster))
            {
                ReplaceCluster((weeds.Source.Value, cluster), ent);
            }
        }
    }

    private void OnTowerRepairAttempt(Entity<CommunicationsTowerComponent> ent, ref RMCRepairableTargetAttemptEvent args)
    {
        if (!ent.Comp.XenoControlled)
            return;

        args.Cancelled = true;
        args.Popup = Loc.GetString("rmc-boon-tower-repair-blocked", ("tower", Name(ent)));
    }

    private void OnCocoonTerminating(Entity<HiveKingCocoonComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp(ent, out TransformComponent? xform) ||
            TerminatingOrDeleted(xform.MapUid) ||
            HasComp<HiveConstructionSuppressAnnouncementsComponent>(ent))
        {
            return;
        }

        var areaName = _area.GetAreaName(ent);
        _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-stopped-marine", ("area", areaName)));
        _xenoAnnounce.AnnounceSameHiveDefaultSound(ent.Owner, Loc.GetString("rmc-boon-king-announcement-stopped-xeno"));
    }

    private void OnBoonTerminating(Entity<HiveBoonDefinitionComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (HasComp<HiveBoonFortificationComponent>(ent.Owner) &&
            !HasOtherActiveFortificationBoon(hive, ent.Owner))
        {
            ClearFortificationRepairing(hive);
        }

        var boons = EnsureBoons(hive);
        foreach (var (id, active) in boons.Comp.Active.ToArray())
        {
            if (active == ent.Owner)
                boons.Comp.Active.Remove(id);
        }

        Dirty(boons);

        if (ent.Comp.Duration <= TimeSpan.Zero ||
            ent.Comp.ExpirationAnnouncement is not { } announcement)
        {
            return;
        }

        _xenoAnnounce.AnnounceSameHiveDefaultSound(ent.Owner, Loc.GetString(announcement));
    }

    public string GetBoonName(EntityPrototype prototype)
    {
        return prototype.Name;
    }

    private string GetBoonName(EntityUid boon)
    {
        if (!TryComp(boon, out HiveBoonDefinitionComponent? comp) ||
            Prototype(boon) is not { } prototype)
        {
            return Name(boon);
        }

        return GetBoonName(prototype);
    }

    private void AnnounceBoonActivation(EntityUid boon)
    {
        if (TryComp(boon, out HiveBoonDefinitionComponent? comp) &&
            comp.ActivationAnnouncement is { } announcement)
        {
            _xenoAnnounce.AnnounceSameHiveDefaultSound(boon, Loc.GetString(announcement));
        }
    }

    private bool CanReceiveTransmuteAction(Entity<XenoComponent> xeno)
    {
        if (xeno.Comp.Tier is <= 0 or > 3 ||
            HasComp<HiveBoonAdaptabilityActionComponent>(xeno.Owner))
        {
            return false;
        }

        return true;
    }

    public bool HasActiveBoon<T>(EntityUid hiveMember) where T : IComponent
    {
        return TryGetActiveBoon<T>(hiveMember, out _);
    }

    public bool TryGetActiveBoon<T>(EntityUid hiveMember, out Entity<T> boon) where T : IComponent
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(uid) ||
                !_hive.FromSameHive(uid, hiveMember))
            {
                continue;
            }

            boon = (uid, comp);
            return true;
        }

        boon = default;
        return false;
    }

    private void HealDamage(EntityUid uid, DamageableComponent damageable, FixedPoint2 amount)
    {
        if (amount <= FixedPoint2.Zero ||
            damageable.TotalDamage <= FixedPoint2.Zero)
        {
            return;
        }

        var heal = -_rmcDamageable.DistributeTypesTotal((uid, damageable), amount);
        _damageable.TryChangeDamage(uid, heal, true, damageable: damageable);
    }

    public bool HasEnoughAliveMarines()
    {
        if (_aliveMarineRequirement <= 0)
            return true;

        var marines = 0;
        var marineQuery = EntityQueryEnumerator<ActorComponent, MarineComponent, TransformComponent>();
        while (marineQuery.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (!_rmcPlanet.IsOnPlanet(xform))
                continue;

            if (_mobState.IsIncapacitated(uid))
                continue;

            marines++;
            if (marines >= _aliveMarineRequirement)
                return true;
        }

        return false;
    }

    private void StartKingVote(Entity<HiveKingCocoonComponent> cocoon)
    {
        _xenoJobs.Clear();
        foreach (var prototype in _prototype.EnumeratePrototypes<PlayTimeTrackerPrototype>())
        {
            if (prototype.IsXeno)
                _xenoJobs.Add(prototype.ID);
        }

        var options = new List<DialogOption>();
        var canVoteList = new List<EntityUid>();
        var netCocoon = GetNetEntity(cocoon);
        var xenosQuery = EntityQueryEnumerator<ActorComponent, XenoComponent>();
        while (xenosQuery.MoveNext(out var uid, out var actor, out _))
        {
            GetKingVotingData((uid, actor), cocoon, out var canBeKing, out var canVote);

            if (canBeKing)
                options.Add(new DialogOption(Name(uid), new HiveKingVoteDialogEvent(netCocoon, GetNetEntity(uid))));

            if (canVote)
                canVoteList.Add(uid);
        }

        foreach (var uid in canVoteList)
        {
            _dialog.OpenOptions(uid,
                Loc.GetString("rmc-boon-king-vote-title"),
                options,
                Loc.GetString("rmc-boon-king-vote-message"));
        }

        EnsureVote(cocoon);
    }

    private void GetKingVotingData(Entity<ActorComponent?> xeno, EntityUid cocoon, out bool canBeKing, out bool canVote)
    {
        canBeKing = false;
        canVote = false;
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        if (_mobState.IsDead(xeno))
            return;

        // TODO RMC14 if banished do not count
        if (!_hive.FromSameHive(xeno.Owner, cocoon))
            return;

        if (_excludedFromKingVoteQuery.TryComp(xeno, out var excluded))
        {
            canBeKing = excluded.CanBeKing;
            canVote = excluded.CanVote;
            return;
        }

        canVote = true;
        IReadOnlyDictionary<string, TimeSpan> playTimes;
        try
        {
            playTimes = _playtime.GetPlayTimes(xeno.Comp.PlayerSession);
        }
        catch
        {
            return;
        }

        var totalTime = TimeSpan.Zero;
        foreach (var (jobId, jobTime) in playTimes)
        {
            if (!_xenoJobs.Contains(jobId))
                continue;

            totalTime += jobTime;
        }

        if (totalTime < _kingVoteCandidateTimeRequired)
            return;

        canBeKing = true;
    }

    public Entity<HiveKingVoteComponent> EnsureVote(EntityUid xeno)
    {
        var query = EntityQueryEnumerator<HiveKingVoteComponent>();
        while (query.MoveNext(out var voteId, out var voteComp))
        {
            if (!_hive.FromSameHive(voteId, xeno))
                continue;

            return (voteId, voteComp);
        }

        var vote = Spawn();
        var comp = EnsureComp<HiveKingVoteComponent>(vote);
        _hive.SetSameHive(xeno, vote);
        return (vote, comp);
    }

    private void ReplaceCluster(Entity<HiveClusterComponent> cluster, Entity<CommunicationsTowerComponent> tower)
    {
        if (tower.Comp.XenoControlled || tower.Comp.State != CommunicationsTowerState.Broken)
            return;

        if (_gameTicker.RoundDuration() < CommunicationTowerXenoTakeoverTime)
            return;

        var newWeedSource = SpawnAtPosition(cluster.Comp.TowerReplaceWith, cluster.Owner.ToCoordinates());
        _hive.SetSameHive(cluster.Owner, newWeedSource);

        _appearance.SetData(tower, WeededEntityLayers.Layer, true);
        tower.Comp.XenoControlled = true;
        Dirty(tower);

        if (TryComp(newWeedSource, out HivePylonComponent? pylon))
        {
            pylon.Tower = tower;
            pylon.NextRoyalResin = _timing.CurTime + _royalResinEvery;
            Dirty(newWeedSource, pylon);

            var areaName = _area.GetAreaName(tower);
            _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-pylon-announcement-marine", ("area", areaName)));
            _xenoAnnounce.AnnounceSameHiveDefaultSound(newWeedSource, Loc.GetString("rmc-boon-pylon-announcement-xeno", ("area", areaName)));
        }

        if (!TryComp(newWeedSource, out XenoWeedsComponent? newWeedSourceComp) ||
            !TryComp(cluster, out XenoWeedsComponent? oldWeeds))
        {
            return;
        }

        foreach (var curWeed in oldWeeds.Spread)
        {
            var curWeedComp = EnsureComp<XenoWeedsComponent>(curWeed);
            curWeedComp.Range = newWeedSourceComp.Range;
            curWeedComp.Source = newWeedSource;
            newWeedSourceComp.Spread.Add(curWeed);
        }

        oldWeeds.Spread.Clear();
        Dirty(cluster, oldWeeds);

        RemComp<XenoWeedsSpreadingComponent>(newWeedSource);
        QueueDel(cluster);
    }

    public void TryActivateBoon(Entity<ManageHiveComponent> manage, EntProtoId<HiveBoonDefinitionComponent> boon)
    {
        if (!_prototype.TryIndex(boon, out var boonProto) ||
            !boonProto.TryGetComponent(out HiveBoonDefinitionComponent? boonComp, _compFactory))
        {
            return;
        }

        var boonName = GetBoonName(boonProto);

        if (_hive.GetHive(manage.Owner) is not { } hive)
            return;

        var boons = EnsureBoons(hive);
        if (boons.Comp.RoyalResin < boonComp.Cost)
        {
            var msg = Loc.GetString("rmc-boon-not-enough-royal-resin",
                ("cost", boonComp.Cost),
                ("current", boons.Comp.RoyalResin));
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        var pylons = 0;
        var pylonQuery = EntityQueryEnumerator<HivePylonComponent>();
        while (pylonQuery.MoveNext(out var uid, out _))
        {
            if (_hive.FromSameHive(uid, manage.Owner))
                pylons++;
        }

        if (pylons < boonComp.Pylons)
        {
            var msg = Loc.GetString("rmc-boon-not-enough-pylons",
                ("cost", boonComp.Pylons),
                ("current", pylons));
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        if (!TryGetUnlockAt(boons, boonProto.ID, out var unlockAt))
            return;

        if (_gameTicker.RoundDuration() < unlockAt)
        {
            var msg = Loc.GetString("rmc-boon-not-enough-time");
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        if (!HasEnoughAliveMarines())
        {
            var msg = Loc.GetString("rmc-boon-not-enough-marines");
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        if (boonComp.NoLivingKing)
        {
            var cocoonQuery = EntityQueryEnumerator<HiveKingCocoonComponent>();
            while (cocoonQuery.MoveNext(out var uid, out _))
            {
                if (TerminatingOrDeleted(uid) ||
                    !_hive.FromSameHive(manage.Owner, uid))
                {
                    continue;
                }

                var msg = Loc.GetString("rmc-boon-only-one-hatchery");
                _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
                return;
            }

            var kingQuery = EntityQueryEnumerator<HiveBoonKingComponent>();
            while (kingQuery.MoveNext(out var uid, out _))
            {
                if (!_mobState.IsDead(uid) &&
                    _hive.FromSameHive(manage.Owner, uid))
                {
                    var msg = Loc.GetString("rmc-boon-only-one-king");
                    _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
                    return;
                }
            }
        }

        var core = _hive.GetHiveCore(hive);
        if (boonComp.RequiresCore && core == null)
        {
            var msg = Loc.GetString("rmc-boon-requires-core");
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        var time = _timing.CurTime;
        if (boons.Comp.UsedAt.TryGetValue(boonProto.ID, out var usedAt))
        {
            var cooldownLeft = usedAt + boonComp.Cooldown - time;
            if (cooldownLeft > TimeSpan.Zero)
            {
                var msg = Loc.GetString("rmc-boon-on-cooldown",
                    ("boon", boonName),
                    ("minutes", (int) cooldownLeft.TotalMinutes));
                _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
                return;
            }
        }

        if (boonComp.DuplicateId is { } duplicateId &&
            boons.Comp.Active.TryGetValue(duplicateId, out var activeBoonId) &&
            !TerminatingOrDeleted(activeBoonId))
        {
            var msg = Loc.GetString("rmc-boon-duplicate-active", ("boon", GetBoonName(activeBoonId)));
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        if (!boonComp.Reusable && boons.Comp.UsedAt.ContainsKey(boonProto.ID))
        {
            var msg = Loc.GetString("rmc-boon-not-reusable", ("boon", boonName));
            _popup.PopupCursor(msg, manage, PopupType.MediumCaution);
            return;
        }

        if (boonComp.Event == null)
            return;

        var ev = (HiveBoonEvent?) _serialization.CreateCopy((object) boonComp.Event, notNullableOverride: true);
        if (ev == null)
            return;

        ev.Hive = hive;
        ev.Core = core;

        boons.Comp.UsedAt[boonProto.ID] = time;
        boons.Comp.RoyalResin = Math.Max(0, boons.Comp.RoyalResin - boonComp.Cost);

        var boonEnt = Spawn(boonProto.ID, MapCoordinates.Nullspace);
        _hive.SetSameHive(manage.Owner, boonEnt);

        EnsureComp<TimedDespawnComponent>(boonEnt).Lifetime = (float) boonComp.Duration.TotalSeconds;

        var activeId = boonComp.DuplicateId ?? boonProto.ID;
        boons.Comp.Active[activeId] = boonEnt;
        Dirty(boons, boons.Comp);

        ev.Boon = boonEnt;
        RaiseLocalEvent((object) ev);
    }

    private void ApplyKingPylonObProtection(EntityUid hiveMember)
    {
        var pylonQuery = EntityQueryEnumerator<HivePylonComponent>();
        while (pylonQuery.MoveNext(out var pylonId, out _))
        {
            if (!_hive.FromSameHive(hiveMember, pylonId))
                continue;

            _area.TrySetCanOrbitalBombardRoofing(pylonId, false);
        }
    }

    private bool TryGetUnlockAt(Entity<HiveBoonsComponent> boons, EntProtoId<HiveBoonDefinitionComponent> boonId, out TimeSpan unlockAt)
    {
        if (!_prototype.TryIndex(boonId, out var boon) ||
            !boon.TryGetComponent(out HiveBoonDefinitionComponent? boonComp, _compFactory))
        {
            unlockAt = TimeSpan.Zero;
            return false;
        }

        if (boons.Comp.UnlockAt.TryGetValue(boonId, out unlockAt))
            return true;

        unlockAt = boonComp.UnlockAt;
        if (boonComp.UnlockAtRandomAdd != TimeSpan.Zero)
            unlockAt += _random.Next(TimeSpan.Zero, boonComp.UnlockAtRandomAdd);

        boons.Comp.UnlockAt[boonId] = unlockAt;
        Dirty(boons);
        return true;
    }

    public Entity<HiveBoonsComponent> EnsureBoons(Entity<HiveComponent> hive)
    {
        var boons = EnsureComp<HiveBoonsComponent>(hive);
        return (hive, boons);
    }

    private void ReloadPrototypes()
    {
        var boons = ImmutableArray.CreateBuilder<(EntityPrototype Prototype, HiveBoonDefinitionComponent Component)>();
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryGetComponent(out HiveBoonDefinitionComponent? comp, _compFactory))
                continue;

            boons.Add((prototype, comp));
        }

        boons.Sort((a, b) => string.Compare(GetBoonName(a.Prototype),
            GetBoonName(b.Prototype),
            StringComparison.InvariantCultureIgnoreCase));
        Boons = boons.ToImmutable();
    }

    private void GainResin()
    {
        try
        {
            var time = _timing.CurTime;
            var pylons = EntityQueryEnumerator<HivePylonComponent>();
            while (pylons.MoveNext(out var uid, out var pylon))
            {
                if (pylon.NextRoyalResin >= time)
                    continue;

                pylon.NextRoyalResin = time + _royalResinEvery;
                Dirty(uid, pylon);

                if (_hive.GetHive(uid) is not { } hive)
                    continue;

                var boons = EnsureBoons(hive);
                boons.Comp.RoyalResin = Math.Clamp(boons.Comp.RoyalResin + 1, 0, boons.Comp.RoyalResinMax);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error gaining royal resin:\n{e}");
        }
    }

    private void UpdateFortifiedStructures()
    {
        var time = _timing.CurTime;
        if (time < _nextFortifiedStructuresUpdate)
            return;

        _nextFortifiedStructuresUpdate = time + FortifiedStructuresUpdateEvery;

        if (!TryGetFortifiedStructureHives(_fortifiedStructureHives))
            return;

        var walls = EntityQueryEnumerator<HiveBoonFortificationRepairingComponent, HiveBoonFortifiableWallComponent, DamageableComponent>();
        while (walls.MoveNext(out var uid, out _, out var wall, out var damageable))
        {
            if (_hive.GetHive(uid) is not { } hive ||
                !_fortifiedStructureHives.Contains(hive.Owner) ||
                damageable.TotalDamage <= FixedPoint2.Zero)
            {
                RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
                continue;
            }

            if (time < wall.NextHealAt)
                continue;

            wall.NextHealAt = time + wall.HealEvery;
            Dirty(uid, wall);
            HealDamage(uid, damageable, wall.Heal);

            if (damageable.TotalDamage <= FixedPoint2.Zero)
                RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
        }

        var clusters = EntityQueryEnumerator<HiveBoonFortificationRepairingComponent, HiveClusterComponent, DamageableComponent>();
        while (clusters.MoveNext(out var uid, out _, out var cluster, out var damageable))
        {
            if (_hive.GetHive(uid) is not { } hive ||
                !_fortifiedStructureHives.Contains(hive.Owner) ||
                damageable.TotalDamage <= FixedPoint2.Zero)
            {
                RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
                continue;
            }

            if (time < cluster.NextFortificationRepairAt)
                continue;

            cluster.NextFortificationRepairAt = time + cluster.FortificationRepairEvery;
            Dirty(uid, cluster);

            HealDamage(uid, damageable, damageable.TotalDamage);
            var ev = new XenoStructureRepairedEvent();
            RaiseLocalEvent(uid, ev);
            RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
        }
    }

    private bool TryGetFortifiedStructureHives(HashSet<EntityUid> hives)
    {
        hives.Clear();

        var boons = EntityQueryEnumerator<HiveBoonFortificationComponent>();
        while (boons.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid) ||
                _hive.GetHive(uid) is not { } hive)
            {
                continue;
            }

            hives.Add(hive.Owner);
        }

        return hives.Count > 0;
    }

    private void QueueFortificationRepair(EntityUid uid, DamageChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Damageable.TotalDamage <= FixedPoint2.Zero)
        {
            RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
            return;
        }

        if (!args.DamageIncreased &&
            args.DamageDelta != null)
        {
            return;
        }

        if (!HasActiveBoon<HiveBoonFortificationComponent>(uid))
            return;

        var wasQueued = HasComp<HiveBoonFortificationRepairingComponent>(uid);
        EnsureComp<HiveBoonFortificationRepairingComponent>(uid);

        if (!wasQueued)
            StartFortificationRepairCooldown(uid);

        _nextFortifiedStructuresUpdate = TimeSpan.Zero;
    }

    private void StartFortificationRepairCooldown(EntityUid uid)
    {
        var time = _timing.CurTime;

        if (TryComp(uid, out HiveBoonFortifiableWallComponent? wall) &&
            time >= wall.NextHealAt)
        {
            wall.NextHealAt = time + wall.HealEvery;
            Dirty(uid, wall);
        }

        if (TryComp(uid, out HiveClusterComponent? cluster) &&
            time >= cluster.NextFortificationRepairAt)
        {
            cluster.NextFortificationRepairAt = time + cluster.FortificationRepairEvery;
            Dirty(uid, cluster);
        }
    }

    private void QueueDamagedFortifiedStructures(EntityUid boon)
    {
        var walls = EntityQueryEnumerator<HiveBoonFortifiableWallComponent, DamageableComponent>();
        while (walls.MoveNext(out var uid, out _, out var damageable))
        {
            if (damageable.TotalDamage <= FixedPoint2.Zero ||
                !_hive.FromSameHive(uid, boon))
            {
                continue;
            }

            EnsureComp<HiveBoonFortificationRepairingComponent>(uid);
        }

        var clusters = EntityQueryEnumerator<HiveClusterComponent, DamageableComponent>();
        while (clusters.MoveNext(out var uid, out _, out var damageable))
        {
            if (damageable.TotalDamage <= FixedPoint2.Zero ||
                !_hive.FromSameHive(uid, boon))
            {
                continue;
            }

            EnsureComp<HiveBoonFortificationRepairingComponent>(uid);
        }
    }

    private void ClearFortificationRepairing(Entity<HiveComponent> hive)
    {
        var repairing = EntityQueryEnumerator<HiveBoonFortificationRepairingComponent>();
        while (repairing.MoveNext(out var uid, out _))
        {
            if (!_hive.IsMember(uid, hive.Owner))
                continue;

            RemCompDeferred<HiveBoonFortificationRepairingComponent>(uid);
        }
    }

    private bool HasOtherActiveFortificationBoon(Entity<HiveComponent> hive, EntityUid terminating)
    {
        var boons = EntityQueryEnumerator<HiveBoonFortificationComponent>();
        while (boons.MoveNext(out var uid, out _))
        {
            if (uid == terminating ||
                TerminatingOrDeleted(uid) ||
                !_hive.IsMember(uid, hive.Owner))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        AnnounceKingUnlock();
        GainResin();
        UpdateFortifiedStructures();

        var cocoonQuery = EntityQueryEnumerator<HiveKingCocoonComponent>();
        while (cocoonQuery.MoveNext(out var cocoonId, out var cocoonComp))
        {
            var pylons = 0;
            var pylonQuery = EntityQueryEnumerator<HivePylonComponent>();
            while (pylonQuery.MoveNext(out var pylonId, out _))
            {
                if (!_hive.FromSameHive(cocoonId, pylonId))
                    continue;

                pylons++;
            }

            if (pylons >= cocoonComp.RequiredPylons)
            {
                if (cocoonComp.LastPylons < cocoonComp.RequiredPylons)
                {
                    var areaName = _area.GetAreaName(cocoonId);
                    _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-resumed-marine",
                        ("area", areaName)));
                    _xenoAnnounce.AnnounceSameHiveDefaultSound(cocoonId, Loc.GetString("rmc-boon-king-announcement-resumed-xeno"));
                }

                cocoonComp.LastPylons = pylons;
                ApplyKingPylonObProtection(cocoonId);
            }
            else
            {
                if (cocoonComp.LastPylons >= cocoonComp.RequiredPylons)
                {
                    var areaName = _area.GetAreaName(cocoonId);
                    _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-paused-marine",
                        ("area", areaName)));
                    _xenoAnnounce.AnnounceSameHiveDefaultSound(cocoonId, Loc.GetString("rmc-boon-king-announcement-paused-xeno"));
                }

                cocoonComp.LastPylons = pylons;
                return;
            }

            cocoonComp.TimeLeft -= TimeSpan.FromSeconds(frameTime);

            if (cocoonComp.TimeLeft > _kingVoteStartTime && !HasEnoughAliveMarines())
            {
                cocoonComp.TimeLeft = _kingVoteStartTime;
                cocoonComp.FirstWarning = true;
                Dirty(cocoonId, cocoonComp);
            }

            if (cocoonComp.TimeLeft <= _kingFirstWarningTime &&
                !cocoonComp.FirstWarning)
            {
                cocoonComp.FirstWarning = true;
                Dirty(cocoonId, cocoonComp);

                var areaName = _area.GetAreaName(cocoonId);
                _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-minutes-marine", ("area", areaName), ("minutes", (int) cocoonComp.TimeLeft.TotalMinutes + 1)));
                _xenoAnnounce.AnnounceSameHiveDefaultSound(cocoonId, Loc.GetString("rmc-boon-king-announcement-minutes-xeno", ("minutes", (int) cocoonComp.TimeLeft.TotalMinutes + 1)));
            }

            if (cocoonComp.TimeLeft > _kingVoteStartTime)
                continue;

            if (!cocoonComp.VoteStarted)
            {
                cocoonComp.VoteStarted = true;
                Dirty(cocoonId, cocoonComp);
                StartKingVote((cocoonId, cocoonComp));
            }

            if (cocoonComp.TimeLeft > _kingVoteStartHatchingTime)
                continue;

            if (!cocoonComp.FinalWarning)
            {
                cocoonComp.FinalWarning = true;
                Dirty(cocoonId, cocoonComp);

                var areaName = _area.GetAreaName(cocoonId);
                _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-seconds-marine",
                    ("area", areaName),
                    ("seconds", (int) cocoonComp.TimeLeft.TotalSeconds + 1)));
                _xenoAnnounce.AnnounceSameHiveDefaultSound(cocoonId,
                    Loc.GetString("rmc-boon-king-announcement-seconds-xeno",
                        ("seconds", (int) cocoonComp.TimeLeft.TotalSeconds + 1)));
            }

            if (cocoonComp.TimeLeft > TimeSpan.Zero)
                continue;

            var vote = EnsureVote(cocoonId);
            var votes = new List<(NetUserId Id, int Votes)>();
            foreach (var (user, userVotes) in vote.Comp.Votes)
            {
                votes.Add((user, userVotes));
            }

            votes = votes.OrderByDescending(a => a.Votes).ToList();
            var king = SpawnAtPosition(cocoonComp.Spawn, cocoonId.ToCoordinates());
            _hive.SetSameHive(cocoonId, king);
            EnsureComp<HiveConstructionSuppressAnnouncementsComponent>(cocoonId);
            QueueDel(cocoonId);

            foreach (var (user, _) in votes)
            {
                _mind.ControlMob(user, king);
                if (TryComp(king, out ActorComponent? actor) && actor.PlayerSession.UserId == user)
                {
                    QueueDel(vote);

                    var areaName = _area.GetAreaName(cocoonId);
                    _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-boon-king-announcement-hatch-marine",
                        ("area", areaName)));
                    _xenoAnnounce.AnnounceSameHiveDefaultSound(cocoonId, Loc.GetString("rmc-boon-king-announcement-hatch-xeno"));
                    return;
                }
            }
        }
    }

    private void AnnounceKingUnlock()
    {
        try
        {
            if (_net.IsClient)
                return;

            if (!_rmcGameTicker.ServerOnlyIsInRound())
                return;

            var query = EntityQueryEnumerator<HiveComponent>();
            while (query.MoveNext(out var uid, out var hive))
            {
                var boons = EnsureBoons((uid, hive));
                if (boons.Comp.KingAnnounced)
                    continue;

                if (!TryGetUnlockAt(boons, KingBoonId, out var unlockAt) ||
                    _gameTicker.RoundDuration() < unlockAt)
                {
                    continue;
                }

                boons.Comp.KingAnnounced = true;
                Dirty(boons);

                var sound = new BioscanComponent().XenoSound;
                _xenoAnnounce.AnnounceToHive(default, uid, Loc.GetString("rmc-boon-king-unlock-announcement"), sound);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error announcing king unlock:\n{e}");
        }
    }
}
