using System.Globalization;
using System.Linq;
using Content.Server._RMC14.Marines;
using Content.Server.GameTicking.Events;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Roles;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Medical.Cryogenics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Roles;

public sealed partial class MarineCommandOverrideSystem : EntitySystem
{
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playtimeManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRankSystem _rankSystem = default!;
    [Dependency] private readonly SquadSystem _squadSystem = default!;

    private static readonly ProtoId<JobPrototype> ExecutiveOfficerJob = "CMExecutiveOfficer";
    private static readonly ProtoId<RankPrototype> PrivateRank = "RMCRankPrivate";
    private static readonly ProtoId<DatasetPrototype> MarineRankHierarchy = "RMCMarineRankHierarchy";
    private static readonly ProtoId<DatasetPrototype> MarineSquadHierarchy = "RMCMarineSquadHierarchy";
    private static readonly ProtoId<AccessGroupPrototype> MarineMainAccess = "MarineMain";

    private bool _enabled;
    private EntityUid _commander;
    private int _seniorAuthorityLevel;
    private bool _accessesAdded;
    private TimeSpan? _adaptationTimerEndTime;
    private TimeSpan? _initialTimerEndTime;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);

        _seniorAuthorityLevel = _prototypes.Index(ExecutiveOfficerJob).MarineAuthorityLevel;

        Subs.CVar(_config, RMCCVars.RMCAutomaticCommanderPromotion, v => _enabled = v, true);
    }

    private void OnRoundStarted(RoundStartingEvent ev)
    {
        _initialTimerEndTime = _gameTiming.CurTime + TimeSpan.FromMinutes(2);
    }
    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        _adaptationTimerEndTime = null;
        _initialTimerEndTime = null;
    }

    public override void Update(float frameTime)
    {
        if (!_enabled)
            return;

        if (_initialTimerEndTime != null && _gameTiming.CurTime >= _initialTimerEndTime.Value)
        {
            _initialTimerEndTime = null;
            CheckForSeniorCommandPresence();
        }

        if (_adaptationTimerEndTime != null && _gameTiming.CurTime >= _adaptationTimerEndTime.Value)
        {
            _adaptationTimerEndTime = null;
            CommanderSelection();
        }
    }

    /// <summary>
    /// Checks if there are any players with a job that has a senior command authority level.
    /// If not, ARES declares the situation and sets a timer for 1 minute.
    /// </summary>
    /// <remarks>
    ///  We will skip the sleep check since it has only been a couple of minutes and this is not a normal situation for senior roles to fall asleep and we hope he will return soon.
    /// </remarks>
    private void CheckForSeniorCommandPresence()
    {
        var ares = _ares.EnsureARES();
        var foundAny = false;

        var query = EntityQueryEnumerator<MarineComponent, OriginalRoleComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var _, out var originalRole, out var _, out var _))
        {
            if (_rankSystem.HasInvalidRank(uid, PrivateRank)) // the player has an invalid rank. the privates are not ready yet...
                continue;

            if (HasComp<CryostorageContainedComponent>(uid) || HasComp<InsideCryoPodComponent>(uid))  // the player is in cryostorage or cryopod
                continue;

            if (originalRole.Job == null || !_prototypes.TryIndex(originalRole.Job.Value, out var jobProto))
                continue;

            if (jobProto.MarineAuthorityLevel == 0)
                continue;

            if (jobProto.MarineAuthorityLevel >= _seniorAuthorityLevel)
            {
                return; // Senior command found, no need to announce anything.
            }

            foundAny = true;
        }

        if (!foundAny) // there is no one 0_0
            return;

        _marineAnnounce.AnnounceARESStaging(ares, Loc.GetString("rmc-marine-command-override-no-senior-command-found"));

        _adaptationTimerEndTime = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Selects a marine to act as the interim operation commander when no senior command is present.
    /// The selection process filters candidates by their authority level, living status, valid ID, readiness
    /// and prioritizes them by:
    /// 1. Highest rank according to the marine rank hierarchy.
    /// 2. Highest squad according to the marine squad hierarchy. We additionally check that if all the selected suitable entities are not in squads (for example, staff officers), then we select the most experienced ones according to the played time for the roles or randomly
    /// 3. If multiple candidates are still tied, we choose the most experienced one in time, otherwise random.
    /// If no valid candidates are found, an appropriate ARES announcement is made.
    /// </summary>
    /// <remarks>
    ///  We will skip the sleep check for the senior commanders since it has only been a couple of minutes and this is not a normal situation for senior roles to fall asleep and we hope he will return soon.
    /// </remarks>
    private void CommanderSelection()
    {
        var ares = _ares.EnsureARES();

        // In fact, List contains only entities with the maximum (the same among themselves) non-zero authority level (MarineAuthorityLevel)
        List<EntityUid> candidates = [];

        var query = EntityQueryEnumerator<MarineComponent, OriginalRoleComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var _, out var originalRole, out var mobState, out var mindContainer))
        {
            var mind = CompOrNull<MindComponent>(mindContainer.Mind);

            if (_rankSystem.HasInvalidRank(uid, PrivateRank)) // the player has an invalid rank. the privates are not ready yet...
                continue;

            if (HasComp<CryostorageContainedComponent>(uid) || HasComp<InsideCryoPodComponent>(uid))  // the player is in cryostorage or cryopod
                continue;

            if (originalRole.Job == null || !_prototypes.TryIndex(originalRole.Job.Value, out var jobProto))
                continue;

            if (jobProto.MarineAuthorityLevel == 0)
                continue;

            if (jobProto.MarineAuthorityLevel >= _seniorAuthorityLevel) // Senior command found
            {
                _marineAnnounce.AnnounceARESStaging(ares, Loc.GetString("rmc-marine-command-override-senior-command-found"));
                return;
            }

            if (mind?.UserId == null) // the player has retired from the entity
                continue;

            if (!TryComp<ActorComponent>(uid, out var actor) || actor.PlayerSession == null) // the player has retired from the round
                continue;

            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (HasValidIdTag(uid, out var idTag) && idTag != null)
            {
                TryAddCandidate(uid, candidates);
            }

        }

        if (candidates.Count == 0) // No candidates found
        {
            _marineAnnounce.AnnounceARESStaging(ares, Loc.GetString("rmc-marine-command-override-no-candidates-found"));
            return;
        }

        if (candidates.Count == 1)
        {
            _commander = candidates[0];
        }
        else if (candidates.Count > 1)
        {
            var highestRankCandidates = _rankSystem.GetEntitiesWithHighestRank(candidates, MarineRankHierarchy);

            if (highestRankCandidates == null) // All entities have invalid rank (in this case it is impossible) or an empty dataset was passed
            {
                _marineAnnounce.AnnounceARESStaging(ares, Loc.GetString("rmc-marine-command-override-no-candidates-found"));
                return;
            }

            if (highestRankCandidates.Count == 1)
            {
                _commander = highestRankCandidates[0];
            }
            else // If there are multiple candidates with the same highest rank, continue with them
            {
                var highestSquadCandidates = _squadSystem.GetEntitiesWithHighestSquad(highestRankCandidates, MarineSquadHierarchy);

                if (highestSquadCandidates == null) // All entities have invalid squad (for example, staff officers) or an empty dataset was passed
                {
                    _commander = PickMostExperiencedEntityOrRandom(highestRankCandidates);
                }
                else if (highestSquadCandidates.Count == 1)
                {
                    _commander = highestSquadCandidates[0];
                }
                else if (highestSquadCandidates.Count > 1)
                {
                    _commander = PickMostExperiencedEntityOrRandom(highestSquadCandidates); // We choose among all entities with the same highest level of authority, squad and rank
                }
            }
        }

        if (HasValidIdTag(_commander, out var finalIdTag) && finalIdTag != null)
            TryAddRequiredAccess(finalIdTag.Value, new HashSet<ProtoId<AccessGroupPrototype>> { MarineMainAccess });

        TryComp<OriginalRoleComponent>(_commander, out var roleComp);
        var jobName = string.Empty;
        if (roleComp != null && roleComp.Job != null && _prototypes.TryIndex(roleComp.Job.Value, out var protoJob))
            jobName = protoJob.LocalizedName;

        var announceText = Loc.GetString("rmc-marine-command-override-commander-chosen",
            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName)),
            ("character", _rankSystem.GetSpeakerFullRankName(_commander) ?? Name(_commander)));

        if (_accessesAdded)
            announceText = $"{announceText}\n{Loc.GetString("rmc-marine-command-override-access-added")}";

        _marineAnnounce.AnnounceARESStaging(ares, announceText, null, null);

    }

    /// <summary>
    /// Attempts to add an entity to the list of commander candidates, keeping only those with the highest marine authority level.
    /// </summary>
    private void TryAddCandidate(EntityUid entity, List<EntityUid> candidates)
    {
        if (!TryComp<OriginalRoleComponent>(entity, out var originalRole) || originalRole.Job == null)
            return;

        if (!_prototypes.TryIndex(originalRole.Job.Value, out var entityJob))
            return;

        if (candidates.Count == 0)
        {
            candidates.Add(entity);
            return;
        }

        var entityLevel = entityJob.MarineAuthorityLevel;

        // Check the maximum level among those already added
        var currentMax = 0;

        foreach (var existing in candidates)
        {
            if (!TryComp<OriginalRoleComponent>(existing, out var existingRole) || existingRole.Job == null)
                continue;

            if (!_prototypes.TryIndex(existingRole.Job.Value, out var existingJob))
                continue;

            var level = existingJob.MarineAuthorityLevel;
            if (level > currentMax)
                currentMax = level;
        }

        if (entityLevel > currentMax)
        {
            candidates.Clear();
            candidates.Add(entity);
        }
        else if (entityLevel == currentMax)
        {
            candidates.Add(entity);
        }
        // If the level is less than the current maximum, we don't add
    }

    /// <summary>
    /// Checks if the entity has a valid ID card matching its name and returns it.
    /// </summary>
    private bool HasValidIdTag(EntityUid entity, out EntityUid? idTag)
    {
        idTag = null;
        var entityName = MetaData(entity).EntityName;

        foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
        {
            if (!TryComp<IdCardComponent>(item, out var tag))
                continue;

            if (tag.FullName != entityName) // not the card owner
                continue;

            idTag = item;
            return true;
        }

        return false; // Player removed his ID to avoid being selected or lost his ID
    }

    /// <summary>
    /// Adds missing access to an ID card if necessary.
    /// </summary>
    private bool TryAddRequiredAccess(EntityUid idCard, HashSet<ProtoId<AccessGroupPrototype>> requiredGroups)
    {
        if (!TryComp<AccessComponent>(idCard, out var accessComp))
            return false;

        var initialAccesses = new HashSet<ProtoId<AccessLevelPrototype>>(accessComp.Tags);
        var accessGroups = accessComp.Groups;

        // List of groups that are missing
        var missingGroups = new HashSet<ProtoId<AccessGroupPrototype>>();

        foreach (var requiredGroup in requiredGroups)
        {
            if (!accessGroups.Contains(requiredGroup))
            {
                missingGroups.Add(requiredGroup); // Add missing groups to Set
            }
        }

        if (missingGroups.Count == 0)
            return true;

        // Adding accesses that make up missing groups
        foreach (var group in missingGroups)
        {
            if (!_prototypes.TryIndex<AccessGroupPrototype>(group, out var groupProto))
                continue;

            accessComp.Tags.UnionWith(groupProto.Tags);

            if (!initialAccesses.SetEquals(accessComp.Tags))
            {
                _accessesAdded = true;
            }
        }

        Dirty(idCard, accessComp);

        return true;
    }

    /// <summary>
    /// Returns the entity with the most playtime in their original job, or picks randomly if tied or unavailable.
    /// </summary>
    private EntityUid PickMostExperiencedEntityOrRandom(List<EntityUid> entities)
    {
        var candidates = new List<(EntityUid Entity, TimeSpan Time)>();

        foreach (var entity in entities)
        {
            if (!TryComp(entity, out OriginalRoleComponent? roleComp) || roleComp == null)
                continue;

            if (roleComp.Job == null || !_prototypes.TryIndex(roleComp.Job.Value, out var job))
                continue;

            if (!TryComp(entity, out MindComponent? mindComp) || mindComp == null)
                continue;

            if (!TryComp<ActorComponent>(entity, out var actor) || actor.PlayerSession == null)
                continue;

            if (_playtimeManager.TryGetTrackerTime(actor.PlayerSession, job.PlayTimeTracker, out var roleTime))
            {
                candidates.Add((entity, roleTime.Value));
            }
        }

        if (candidates.Count == 0)
            return _random.Pick(entities); // fallback

        var maxTime = candidates.Max(c => c.Time);
        var topCandidates = candidates.Where(c => c.Time == maxTime).Select(c => c.Entity).ToList();

        return _random.Pick(topCandidates);
    }
}
