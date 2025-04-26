using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Roles;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking.Events;
using Robust.Shared.Timing;
using Content.Shared._RMC14.ARES;
using Content.Server._RMC14.Marines;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mind;
using Content.Shared.Bed.Cryostorage;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Inventory;
using Content.Shared.Access.Components;
using Content.Shared.Access;
using System.Linq;
using Content.Shared.Dataset;

namespace Content.Server._RMC14.Roles;

public sealed partial class MarineCommandOverrideSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedRankSystem _rankSystem = default!;


    private const int AuthorityThreshold = 13;
    private TimeSpan? _adaptationTimerEndTime;
    private TimeSpan? _initialTimerEndTime;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartingEvent ev)
    {
        _initialTimerEndTime = _gameTiming.CurTime + TimeSpan.FromMinutes(2);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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
        bool foundAny = false;

        var query = EntityQueryEnumerator<MarineComponent, OriginalRoleComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var _, out var originalRole, out var _, out var _))
        {
            if (HasInvalidRank(uid)) // the player has an invalid rank
                continue;

            if (IsInAnyCryostorage(uid)) // the player is in cryostorage
                continue;

            if (originalRole.Job == null || !_prototypes.TryIndex(originalRole.Job.Value, out JobPrototype? jobProto))
                continue;

            if (jobProto.MarineAuthorityLevel == 0)
                continue;

            if (jobProto.MarineAuthorityLevel >= AuthorityThreshold)
            {
                return; // Senior command found, no need to announce anything.
            }

            foundAny = true;
        }

        if (!foundAny) // there is no one 0_0
            return;

        _marineAnnounce.AnnounceARES(ares, Loc.GetString("rmc-marine-command-override-no-senior-command-found"));

        _adaptationTimerEndTime = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
    }

    private void CommanderSelection()
    {
        var ares = _ares.EnsureARES();
        List<EntityUid> candidates = [];

        var query = EntityQueryEnumerator<MarineComponent, OriginalRoleComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var _, out var originalRole, out var mobState, out var mindContainer))
        {
            var mind = CompOrNull<MindComponent>(mindContainer.Mind);

            if (HasInvalidRank(uid)) // the player has an invalid rank
                continue;

            if (IsInAnyCryostorage(uid)) // the player is in cryostorage
                continue;

            if (originalRole.Job == null || !_prototypes.TryIndex(originalRole.Job.Value, out JobPrototype? jobProto))
                continue;

            if (jobProto.MarineAuthorityLevel == 0)
                continue;

            if (jobProto.MarineAuthorityLevel >= AuthorityThreshold) // Senior command found
            {
                _marineAnnounce.AnnounceARES(ares, Loc.GetString("rmc-marine-command-override-senior-command-found"));
                return;
            }

            if (mind?.UserId == null || mind?.Session == null) // the player has retired from the round
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
            _marineAnnounce.AnnounceARES(ares, Loc.GetString("rmc-marine-command-override-no-сandidates-found"));
            return;
        }

        if (candidates.Count == 1 && HasValidIdTag(candidates[0], out var finalIdTag) && finalIdTag != null)
        {
            TryAddRequiredAccess(finalIdTag.Value, new HashSet<ProtoId<AccessGroupPrototype>> { new ProtoId<AccessGroupPrototype>("MarineMain") });
        }

        if (candidates.Count > 1)
        {
            EntityUid commander;


            _rankSystem.GetEntitiesWithHighestRank(candidates, "RMCMarineRankHierarchy");



            if (!_prototypes.TryIndex<DatasetPrototype>("RMCMarineRankHierarchy", out var rankHierarchy))
                return;

            // Получаем список всех рангов и их старшинства
            var rankOrder = rankHierarchy.Values.ToList();

            // Словарь для хранения текущих сущностей с их рангами
            Dictionary<EntityUid, int> rankScores = new Dictionary<EntityUid, int>();

            // Сравниваем и находим сущности с самыми высокими рангами
            int highestRank = -1;

            foreach (var candidate in candidates)
            {
                if (!TryComp<RankComponent>(candidate, out var rankComp) || rankComp.Rank == null)
                    continue;

                if (!_prototypes.TryIndex<RankPrototype>(rankComp.Rank, out var rankProto))
                    continue;

                // Определяем старшинство ранга
                int rankIndex = rankOrder.IndexOf(rankProto.ID);
                if (rankIndex == -1)
                    continue;

                // Сохраняем старшинство для кандидата
                rankScores[candidate] = rankIndex;

                if (rankIndex > highestRank)
                    highestRank = rankIndex;
            }

            // Отбираем кандидатов с самым высоким рангом
            var highestRankCandidates = rankScores.Where(pair => pair.Value == highestRank).Select(pair => pair.Key).ToList();

            // Если несколько кандидатов с одинаковым самым высоким рангом, продолжаем с ними
            if (highestRankCandidates.Count == 1 && HasValidIdTag(highestRankCandidates[0], out var idTag) && idTag != null)
            {
                TryAddRequiredAccess(idTag.Value, new HashSet<ProtoId<AccessGroupPrototype>> { new ProtoId<AccessGroupPrototype>("MarineMain") });
                commander = highestRankCandidates[0];
            }
            else
            {
                // Если несколько кандидатов, нужно решить, что делать дальше
            }

            _marineAnnounce.AnnounceARES(ares,
                Loc.GetString("rmc-latejoin-arrival-announcement-special",
                ("character", fullRankName)),
                null,
                null);
        }

    }

    private void TryAddCandidate(EntityUid entity, List<EntityUid> candidates)
    {
        if (!TryComp<OriginalRoleComponent>(entity, out var originalRole) || originalRole.Job == null)
            return;

        if (!_prototypes.TryIndex(originalRole.Job.Value, out JobPrototype? entityJob))
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

            if (!_prototypes.TryIndex(existingRole.Job.Value, out JobPrototype? existingJob))
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
        // If the level is less than the current maximum, we don’t add
    }

    private bool IsInAnyCryostorage(EntityUid target)
    {
        var cryoQuery = EntityQueryEnumerator<CryostorageContainedComponent>();
        while (cryoQuery.MoveNext(out var _, out var comp))
        {
            if (comp.Cryostorage != null && comp.Cryostorage == target)
                return true;
        }

        return false;
    }

    private bool HasInvalidRank(EntityUid entity)
    {
        if (!_entMan.TryGetComponent<RankComponent>(entity, out var rankComp))
            return true;

        if (rankComp.Rank == null)
            return true;

        return rankComp.Rank == "RMCRankPrivate"; // the privates are not ready yet...
    }

    private bool HasValidIdTag(EntityUid entity, out EntityUid? idTag)
    {
        idTag = null;
        var entityName = _entMan.GetComponent<MetaDataComponent>(entity).EntityName;

        foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
        {
            if (!_entMan.TryGetComponent<IdCardComponent>(item, out var tag))
                continue;

            if (tag.FullName != entityName) // not the card owner
                continue;

            idTag = item;
            return true;
        }

        return false;
    }

    private bool TryAddRequiredAccess(EntityUid idCard, HashSet<ProtoId<AccessGroupPrototype>> requiredGroups)
    {
        if (!_entMan.TryGetComponent<AccessComponent>(idCard, out var accessComp))
            return false;

        // Проверим, содержатся ли все требуемые группы
        var accessGroups = accessComp.Groups;

        // Список групп, которых не хватает
        var missingGroups = new HashSet<ProtoId<AccessGroupPrototype>>();

        foreach (var requiredGroup in requiredGroups)
        {
            if (!accessGroups.Contains(requiredGroup))
            {
                missingGroups.Add(requiredGroup); // Добавляем отсутствующие группы в Set
            }
        }

        if (missingGroups.Count == 0)
            return true;

        // Добавляем доступы, составляющие упущенные группы
        foreach (var group in missingGroups)
        {
            if (!_prototypes.TryIndex<AccessGroupPrototype>(group, out var groupProto))
                continue;

            accessComp.Tags.UnionWith(groupProto.Tags); // Также добавляем теги
        }

        Dirty(idCard, accessComp); // Уведомим, что компонент изменился

        return true;
    }



}
