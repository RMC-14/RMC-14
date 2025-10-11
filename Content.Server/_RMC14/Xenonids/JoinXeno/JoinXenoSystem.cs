using Content.Server.Mind;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._RMC14.Xenonids.JoinXeno;

public sealed class JoinXenoSystem : SharedJoinXenoSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasiteSystem = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private ISawmill _logger = default!;

    public override void Initialize()
    {
        base.Initialize();

        _logger = Logger.GetSawmill("xeno.queue");

        SubscribeLocalEvent<LarvaReadyToBurstEvent>(OnLarvaReadyToBurst);
        SubscribeLocalEvent<BurstLarvaConsumedEvent>(OnBurstLarvaConsumed);
        SubscribeLocalEvent<LarvaQueueComponent, ComponentStartup>(OnLarvaQueueStartup);
        SubscribeLocalEvent<LarvaQueueComponent, ScanExistingLarvaeEvent>(OnScanExistingLarvae);
        SubscribeLocalEvent<LarvaPriorityComponent, ComponentStartup>(OnLarvaPriorityStartup);
        SubscribeLocalEvent<AssignLarvaPriorityEvent>(OnAssignLarvaPriority);
        SubscribeLocalEvent<LarvaPriorityCompletedEvent>(OnLarvaPriorityCompleted);
    }

    private void OnLarvaPriorityStartup(Entity<LarvaPriorityComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.HasPriorityPlayers)
            ProcessLarvaPriority((ent.Owner, ent.Comp));
    }

    private void OnAssignLarvaPriority(ref AssignLarvaPriorityEvent args)
    {
        var priorityComp = EnsureComp<LarvaPriorityComponent>(args.Larva);
        priorityComp.OriginalParasiteUserId = args.OriginalParasiteUserId;
        priorityComp.BurstVictimUserId = args.BurstVictimUserId;
        priorityComp.HasPriorityPlayers = args.OriginalParasiteUserId != null || args.BurstVictimUserId != null;
        Dirty(args.Larva, priorityComp);

        if (priorityComp.HasPriorityPlayers)
            ProcessLarvaPriority((args.Larva, priorityComp));
    }

    private void OnLarvaPriorityCompleted(ref LarvaPriorityCompletedEvent args)
    {
        if (!TryComp<LarvaPriorityComponent>(args.Larva, out var priority))
            return;

        if (!args.WasAccepted)
            AddLarvaToNormalQueue(args.Larva);

        RemCompDeferred<LarvaPriorityComponent>(args.Larva);
    }

    private void ProcessLarvaPriority(Entity<LarvaPriorityComponent> larva)
    {
        var hive = _hive.GetHive(larva.Owner);
        if (hive == null)
            return;

        if (ShouldOfferToParasite(larva.Comp))
        {
            if (TryOfferLarvaToPriorityPlayer(larva, hive.Value, larva.Comp.OriginalParasiteUserId!.Value))
            {
                larva.Comp.ParasiteOffered = true;
                Dirty(larva.Owner, larva.Comp);
                return;
            }
            larva.Comp.ParasiteOffered = true;
            Dirty(larva.Owner, larva.Comp);
        }

        if (ShouldOfferToVictim(larva.Comp))
        {
            if (TryOfferLarvaToPriorityPlayer(larva, hive.Value, larva.Comp.BurstVictimUserId!.Value))
            {
                larva.Comp.VictimOffered = true;
                Dirty(larva.Owner, larva.Comp);
                return;
            }
            larva.Comp.VictimOffered = true;
            Dirty(larva.Owner, larva.Comp);
        }

        AddLarvaToNormalQueue(larva.Owner);
        RemCompDeferred<LarvaPriorityComponent>(larva.Owner);
    }

    private bool ShouldOfferToParasite(LarvaPriorityComponent comp)
    {
        return comp.OriginalParasiteUserId != null && !comp.ParasiteOffered;
    }

    private bool ShouldOfferToVictim(LarvaPriorityComponent comp)
    {
        return comp.BurstVictimUserId != null && !comp.VictimOffered;
    }

    private bool TryOfferLarvaToPriorityPlayer(Entity<LarvaPriorityComponent> larva, Entity<HiveComponent> hive, NetUserId userId)
    {
        if (!TryGetValidSession(userId, out var session))
            return false;

        var promptComp = EnsureComp<LarvaQueuePromptComponent>(hive.Owner);

        if (promptComp.PendingPrompts.ContainsKey(userId))
            return false;

        CreatePrompt(promptComp, hive, larva.Owner, userId);
        return true;
    }

    private bool TryGetValidSession(NetUserId userId, out ICommonSession? session)
    {
        session = null;
        if (!_playerManager.TryGetSessionById(userId, out session))
            return false;

        return CanStayInQueue(session);
    }

    private void CreatePrompt(LarvaQueuePromptComponent promptComp, Entity<HiveComponent> hive, EntityUid larva, NetUserId userId)
    {
        var expiryTime = _timing.CurTime + promptComp.PromptTimeout;
        var larvaNetEntity = GetNetEntity(larva);
        promptComp.PendingPrompts[userId] = (larvaNetEntity, expiryTime);

        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            var promptEvent = new LarvaPromptEvent(larvaNetEntity, GetNetEntity(hive.Owner), expiryTime);
            RaiseNetworkEvent(promptEvent, session);
        }

        Dirty(hive.Owner, promptComp);
    }

    private void AddLarvaToNormalQueue(EntityUid larva)
    {
        var hive = _hive.GetHive(larva);
        if (hive == null)
            return;

        var queue = EnsureComp<LarvaQueueComponent>(hive.Value.Owner);

        if (queue.PendingLarvae.Contains(larva))
            return;

        queue.PendingLarvae.Add(larva);
        Dirty(hive.Value.Owner, queue);

        if (queue.PlayerQueue.Count > 0)
            ProcessLarvaQueue(hive.Value, (hive.Value.Owner, queue));
    }

    private bool CanStayInQueue(ICommonSession session)
    {
        if (session.AttachedEntity == null)
            return false;

        var entity = session.AttachedEntity.Value;

        return HasComp<GhostComponent>(entity) ||
               HasComp<XenoParasiteComponent>(entity) ||
               HasComp<VictimBurstComponent>(entity) ||
               HasComp<VictimInfectedComponent>(entity) ||
               (TryComp<XenoComponent>(entity, out var xeno) && xeno.Role.Id == "CMXenoLesserDrone"); //cursed ahh code
    }

    private void CleanupInvalidQueuePlayers(Entity<LarvaQueueComponent> queue)
    {
        var validPlayers = new Queue<NetUserId>();
        var removedCount = 0;

        while (queue.Comp.PlayerQueue.Count > 0)
        {
            var userId = queue.Comp.PlayerQueue.Dequeue();

            if (!_playerManager.TryGetSessionById(userId, out var session))
            {
                removedCount++;
                continue;
            }

            if (CanStayInQueue(session))
            {
                validPlayers.Enqueue(userId);
            }
            else
            {
                removedCount++;
                if (session.AttachedEntity != null)
                {
                    _popup.PopupEntity(
                        Loc.GetString("rmc-xeno-queue-removed-invalid-state"),
                        session.AttachedEntity.Value,
                        session.AttachedEntity.Value,
                        PopupType.MediumCaution);
                }
            }
        }

        queue.Comp.PlayerQueue = validPlayers;

        if (removedCount > 0)
            Dirty(queue);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<HiveComponent, LarvaQueueComponent>();

        while (query.MoveNext(out var hiveId, out var hive, out var queue))
        {
            CleanupInvalidQueuePlayers((hiveId, queue));
            ProcessExpiredPrompts((hiveId, hive), (hiveId, queue), time);

            if (queue.PlayerQueue.Count == 0)
                continue;

            if (time - queue.LastProcessed < TimeSpan.FromSeconds(queue.ProcessInterval))
                continue;

            ProcessLarvaQueue((hiveId, hive), (hiveId, queue));
            queue.LastProcessed = time;
            Dirty(hiveId, queue);
        }
    }

    private void ProcessExpiredPrompts(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue, TimeSpan currentTime)
    {
        if (!TryComp<LarvaQueuePromptComponent>(hive.Owner, out var promptComp))
            return;

        var expiredPrompts = promptComp.PendingPrompts
            .Where(kvp => currentTime >= kvp.Value.ExpiresAt)
            .Select(kvp => new ExpiredPrompt(kvp.Key, kvp.Value.Larva))
            .ToList();

        if (expiredPrompts.Count == 0)
            return;

        foreach (var expired in expiredPrompts)
        {
            promptComp.PendingPrompts.Remove(expired.UserId);
            HandleExpiredPrompt(expired, queue);
            NotifyPlayerOfTimeout(expired);
        }

        Dirty(hive.Owner, promptComp);
        Dirty(queue.Owner, queue.Comp);
        SendQueueStatusToAll(hive, queue);
    }

    private void HandleExpiredPrompt(ExpiredPrompt expired, Entity<LarvaQueueComponent> queue)
    {
        if (!TryGetEntity(expired.LarvaNetEntity, out var larva))
            return;

        if (TryComp<LarvaPriorityComponent>(larva.Value, out var priorityComp))
        {
            ProcessLarvaPriority((larva.Value, priorityComp));
        }
        else
        {
            ReturnLarvaToQueue(larva.Value, queue);
        }
    }

    private void ReturnLarvaToQueue(EntityUid larva, Entity<LarvaQueueComponent> queue)
    {
        if (TerminatingOrDeleted(larva) || _mobState.IsDead(larva))
            return;

        if (!queue.Comp.PendingLarvae.Contains(larva))
            queue.Comp.PendingLarvae.Add(larva);
    }

    private void NotifyPlayerOfTimeout(ExpiredPrompt expired)
    {
        if (!_playerManager.TryGetSessionById(expired.UserId, out var session))
            return;

        var cancelEv = new LarvaPromptCancelledEvent(expired.LarvaNetEntity, "timeout");
        RaiseNetworkEvent(cancelEv, session);

        _popup.PopupEntity(
            Loc.GetString("rmc-xeno-larva-prompt-timeout"),
            session.AttachedEntity ?? EntityUid.Invalid,
            session.AttachedEntity ?? EntityUid.Invalid,
            PopupType.MediumCaution);

        var query = EntityQueryEnumerator<HiveComponent, LarvaQueueComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var queue))
        {
            if (!queue.PlayerQueue.Contains(expired.UserId))
            {
                queue.PlayerQueue.Enqueue(expired.UserId);
                Dirty(hiveId, queue);
                SendQueueStatusToAll((hiveId, hive), (hiveId, queue));
                break;
            }
        }
    }

    private void OnLarvaQueueStartup(Entity<LarvaQueueComponent> queue, ref ComponentStartup args)
    {
        ScanForExistingLarvae(queue);
    }

    private void OnScanExistingLarvae(Entity<LarvaQueueComponent> queue, ref ScanExistingLarvaeEvent args)
    {
        ScanForExistingLarvae(queue);
    }

    private void ScanForExistingLarvae(Entity<LarvaQueueComponent> queue)
    {
        var foundLarvae = new List<EntityUid>();
        var infectedQuery = EntityQueryEnumerator<VictimInfectedComponent>();

        while (infectedQuery.MoveNext(out var hostId, out var infected))
        {
            if (ShouldAddLarvaFromScan(infected, queue, out var larva) && larva.HasValue)
                foundLarvae.Add(larva.Value);
        }

        if (foundLarvae.Count == 0)
            return;

        foreach (var larva in foundLarvae)
            queue.Comp.PendingLarvae.Add(larva);

        Dirty(queue);
        if (queue.Comp.PlayerQueue.Count > 0 && TryComp<HiveComponent>(queue.Owner, out var hive))
            ProcessLarvaQueue((queue.Owner, hive), queue);
    }

    private bool ShouldAddLarvaFromScan(VictimInfectedComponent infected, Entity<LarvaQueueComponent> queue, out EntityUid? larva)
    {
        larva = null;

        if (infected.SpawnedLarva == null || infected.IsBursting)
            return false;

        larva = infected.SpawnedLarva.Value;
        var larvaHive = _hive.GetHive(larva.Value);

        if (larvaHive?.Owner != queue.Owner)
            return false;

        return !queue.Comp.PendingLarvae.Contains(larva.Value) && !HasComp<LarvaPriorityComponent>(larva.Value);
    }

    private void OnLarvaReadyToBurst(ref LarvaReadyToBurstEvent args)
    {
        var hive = _hive.GetHive(args.Larva);
        if (hive == null)
            return;

        var queue = EnsureComp<LarvaQueueComponent>(hive.Value.Owner);

        if (HasComp<LarvaPriorityComponent>(args.Larva))
        {
            SendQueueStatusToAll(hive.Value, (hive.Value.Owner, queue));
            return;
        }

        queue.PendingLarvae.Add(args.Larva);
        Dirty(hive.Value.Owner, queue);

        if (queue.PlayerQueue.Count > 0)
            ProcessLarvaQueue(hive.Value, (hive.Value.Owner, queue));

        SendQueueStatusToAll(hive.Value, (hive.Value.Owner, queue));
    }

    private void OnBurstLarvaConsumed(ref BurstLarvaConsumedEvent args)
    {
        var query = EntityQueryEnumerator<LarvaQueueComponent>();
        while (query.MoveNext(out var queueId, out var queue))
        {
            if (!queue.PendingLarvae.Remove(args.Larva))
                continue;

            Dirty(queueId, queue);

            if (TryComp<HiveComponent>(queueId, out var hive))
                SendQueueStatusToAll((queueId, hive), (queueId, queue));

            break;
        }
    }

    private void ProcessLarvaQueue(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue)
    {
        CleanupStaleLarvae(queue);

        if (queue.Comp.PlayerQueue.Count == 0)
            return;

        var availableCount = CountAvailableLarvae(queue.Comp, hive.Comp);
        if (availableCount <= 0)
            return;

        var promptComp = EnsureComp<LarvaQueuePromptComponent>(hive.Owner);
        var processed = ProcessQueuedPlayers(hive, queue, promptComp, availableCount);

        if (processed > 0)
        {
            Dirty(hive.Owner, promptComp);
            Dirty(queue);
            SendQueueStatusToAll(hive, queue);
        }
    }

    private int CountAvailableLarvae(LarvaQueueComponent queue, HiveComponent hive)
    {
        var normalLarvae = queue.PendingLarvae.Count(larva =>
            !TryComp<LarvaPriorityComponent>(larva, out var priority) || !priority.HasPriorityPlayers);

        return hive.BurrowedLarva + normalLarvae;
    }

    private int ProcessQueuedPlayers(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue, LarvaQueuePromptComponent promptComp, int availableCount)
    {
        var processed = 0;
        while (processed < availableCount && queue.Comp.PlayerQueue.Count > 0)
        {
            var userId = queue.Comp.PlayerQueue.Dequeue();
            if (!TryGetValidSession(userId, out var session))
            {
                processed++;
                continue;
            }

            if (promptComp.PendingPrompts.ContainsKey(userId))
            {
                queue.Comp.PlayerQueue.Enqueue(userId);
                continue;
            }

            if (!TryGetOrSpawnLarva(hive, queue, out var larva) || !larva.HasValue)
            {
                queue.Comp.PlayerQueue.Enqueue(userId);
                break;
            }

            CreatePrompt(promptComp, hive, larva.Value, userId);
            processed++;
        }

        return processed;
    }

    private bool TryGetOrSpawnLarva(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue, out EntityUid? larva)
    {
        larva = null;

        foreach (var candidate in queue.Comp.PendingLarvae)
        {
            if (!TryComp<LarvaPriorityComponent>(candidate, out var priority) || !priority.HasPriorityPlayers)
            {
                larva = candidate;
                queue.Comp.PendingLarvae.Remove(candidate);
                return true;
            }
        }

        if (hive.Comp.BurrowedLarva > 0 && TrySpawnBurrowedLarva(hive, out larva))
            return true;

        return false;
    }

    private bool TrySpawnBurrowedLarva(Entity<HiveComponent> hive, out EntityUid? larva)
    {
        larva = null;

        if (hive.Comp.BurrowedLarva <= 0)
            return false;

        if (!TryFindSpawnLocation(hive, out var spawnCoords))
            return false;

        var spawnedLarva = Spawn(hive.Comp.BurrowedLarvaId, spawnCoords);
        _hive.IncreaseBurrowedLarva(hive, -1);
        _xeno.MakeXeno(spawnedLarva);
        _hive.SetHive(spawnedLarva, hive.Owner);

        larva = spawnedLarva;
        return true;
    }

    private bool TryFindSpawnLocation(Entity<HiveComponent> hive, out EntityCoordinates coords)
    {
        coords = default;

        if (TryFindSpawnAt<HiveCoreComponent>(hive, out coords))
            return true;

        if (TryFindSpawnAt<XenoEvolutionGranterComponent>(hive, out coords))
            return true;

        if (TryFindSpawnAt<XenoComponent>(hive, out coords))
            return true;

        return false;
    }

    private bool TryFindSpawnAt<T>(Entity<HiveComponent> hive, out EntityCoordinates coords) where T : Component
    {
        coords = default;
        var candidates = EntityQueryEnumerator<T, HiveMemberComponent>();

        while (candidates.MoveNext(out var uid, out _, out var member))
        {
            if (member.Hive != hive.Owner || _mobState.IsDead(uid))
                continue;

            coords = Transform(uid).Coordinates;
            return true;
        }

        return false;
    }

    private void CleanupStaleLarvae(Entity<LarvaQueueComponent> queue)
    {
        var staleLarvae = queue.Comp.PendingLarvae
            .Where(larva => TerminatingOrDeleted(larva) || _mobState.IsDead(larva))
            .ToList();

        if (staleLarvae.Count == 0)
            return;

        foreach (var larva in staleLarvae)
            queue.Comp.PendingLarvae.Remove(larva);

        Dirty(queue);
    }

    private bool TryAssignLarva(Entity<HiveComponent> hive, EntityUid larva, ICommonSession session)
    {
        if (TerminatingOrDeleted(larva) || _mobState.IsDead(larva))
            return false;

        if (!TryComp<MetaDataComponent>(larva, out var metaData))
            return false;

        var newMind = _mind.CreateMind(session.UserId, metaData.EntityName);
        _mind.TransferTo(newMind, larva, ghostCheckOverride: true);

        if (TryComp<BursterComponent>(larva, out var burster) &&
            TryComp<VictimInfectedComponent>(burster.BurstFrom, out var infected))
        {
            _parasiteSystem.TryStartBurst((burster.BurstFrom, infected));
        }

        var consumedEv = new BurstLarvaConsumedEvent(larva);
        RaiseLocalEvent(ref consumedEv);

        return true;
    }

    protected override void OnAcceptLarvaPrompt(AcceptLarvaPromptRequest msg, EntitySessionEventArgs args)
    {
        _logger.Info($"OnAcceptLarvaPrompt: Player {args.SenderSession.UserId} accepting larva {msg.Larva}");

        if (!TryGetEntity(msg.Larva, out var larva))
        {
            _logger.Info($"OnAcceptLarvaPrompt: Could not resolve larva entity {msg.Larva}");
            return;
        }

        var query = EntityQueryEnumerator<HiveComponent, LarvaQueuePromptComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var promptComp))
        {
            if (!ValidatePrompt(promptComp, args.SenderSession.UserId, msg.Larva, out var expiryTime))
                continue;

            if (_timing.CurTime >= expiryTime)
            {
                _logger.Info($"OnAcceptLarvaPrompt: Prompt expired for player {args.SenderSession.UserId}");
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-larva-prompt-expired"),
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    PopupType.MediumCaution);
                return;
            }

            _logger.Info($"OnAcceptLarvaPrompt: Valid prompt found, assigning larva {ToPrettyString(larva.Value)} to player {args.SenderSession.UserId}");
            promptComp.PendingPrompts.Remove(args.SenderSession.UserId);
            Dirty(hiveId, promptComp);

            HandleLarvaAcceptance((hiveId, hive), larva.Value, args.SenderSession);

            if (TryComp<LarvaQueueComponent>(hiveId, out var queueComp))
                SendQueueStatusToAll((hiveId, hive), (hiveId, queueComp));

            return;
        }

        _logger.Info($"OnAcceptLarvaPrompt: No valid prompt found for player {args.SenderSession.UserId}");
    }

    private bool ValidatePrompt(LarvaQueuePromptComponent promptComp, NetUserId userId, NetEntity larvaNet, out TimeSpan expiryTime)
    {
        expiryTime = default;

        if (!promptComp.PendingPrompts.TryGetValue(userId, out var promptData))
            return false;

        if (promptData.Larva != larvaNet)
            return false;

        expiryTime = promptData.ExpiresAt;
        return true;
    }

    private void HandleLarvaAcceptance(Entity<HiveComponent> hive, EntityUid larva, ICommonSession session)
    {
        if (TryAssignLarva(hive, larva, session))
        {
            _rmcGameTicker.PlayerJoinGame(session);
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-larva-accepted"),
                session.AttachedEntity ?? EntityUid.Invalid,
                session.AttachedEntity ?? EntityUid.Invalid,
                PopupType.Medium);

            if (TryComp<LarvaPriorityComponent>(larva, out _))
            {
                var completedEv = new LarvaPriorityCompletedEvent(larva, true, session.UserId);
                RaiseLocalEvent(ref completedEv);
            }
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-larva-assignment-failed"),
                session.AttachedEntity ?? EntityUid.Invalid,
                session.AttachedEntity ?? EntityUid.Invalid,
                PopupType.MediumCaution);
            HandleFailedAssignment(hive, larva);
        }
    }

    private void HandleFailedAssignment(Entity<HiveComponent> hive, EntityUid larva)
    {
        if (TryComp<LarvaPriorityComponent>(larva, out var priority))
        {
            ProcessLarvaPriority((larva, priority));
        }
        else if (TryComp<LarvaQueueComponent>(hive.Owner, out var queue))
        {
            if (!TerminatingOrDeleted(larva) && !_mobState.IsDead(larva))
            {
                queue.PendingLarvae.Add(larva);
                Dirty(hive.Owner, queue);
            }
        }
    }

    protected override void OnDeclineLarvaPrompt(DeclineLarvaPromptRequest msg, EntitySessionEventArgs args)
    {
        _logger.Info($"OnDeclineLarvaPrompt: Player {args.SenderSession.UserId} declining larva {msg.Larva}");

        if (!TryGetEntity(msg.Larva, out var larva))
        {
            _logger.Info($"OnDeclineLarvaPrompt: Could not resolve larva entity {msg.Larva}");
            return;
        }

        var query = EntityQueryEnumerator<HiveComponent, LarvaQueuePromptComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var promptComp))
        {
            if (!ValidatePrompt(promptComp, args.SenderSession.UserId, msg.Larva, out _))
                continue;

            _logger.Info($"OnDeclineLarvaPrompt: Valid prompt found, handling decline for larva {ToPrettyString(larva.Value)}");
            promptComp.PendingPrompts.Remove(args.SenderSession.UserId);
            Dirty(hiveId, promptComp);

            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-larva-declined"),
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                PopupType.Medium);
            HandleLarvaDecline((hiveId, hive), larva.Value, args.SenderSession.UserId);

            return;
        }

        _logger.Info($"OnDeclineLarvaPrompt: No valid prompt found for player {args.SenderSession.UserId}");
    }

    private void HandleLarvaDecline(Entity<HiveComponent> hive, EntityUid larva, NetUserId userId)
    {
        if (TryComp<LarvaPriorityComponent>(larva, out var priority))
        {
            ProcessLarvaPriority((larva, priority));
        }
        else if (TryComp<LarvaQueueComponent>(hive.Owner, out var queue))
        {
            if (!TerminatingOrDeleted(larva) && !_mobState.IsDead(larva))
                queue.PendingLarvae.Add(larva);

            Dirty(hive.Owner, queue);
            SendQueueStatusToAll(hive, (hive.Owner, queue));
        }
    }

    protected override void OnJoinLarvaQueueRequest(JoinLarvaQueueRequest msg, EntitySessionEventArgs args)
    {
        if (_rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(args.SenderSession.UserId) == PlayerGameStatus.JoinedGame)
            return;

        if (!CanStayInQueue(args.SenderSession))
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-queue-invalid-state"),
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                PopupType.MediumCaution);
            return;
        }

        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive))
                continue;

            if (!TryJoinQueue(args.SenderSession, comp.Hive, hive))
                return;

            break;
        }
    }

    private bool TryJoinQueue(ICommonSession session, EntityUid hiveId, HiveComponent hive)
    {
        var queue = EnsureComp<LarvaQueueComponent>(hiveId);

        if (queue.PlayerQueue.Contains(session.UserId))
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-queue-already-in"),
                session.AttachedEntity ?? EntityUid.Invalid,
                session.AttachedEntity ?? EntityUid.Invalid,
                PopupType.MediumCaution);
            return false;
        }

        if (queue.PlayerQueue.Count >= queue.MaxQueueSize)
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-queue-full"),
                session.AttachedEntity ?? EntityUid.Invalid,
                session.AttachedEntity ?? EntityUid.Invalid,
                PopupType.MediumCaution);
            return false;
        }

        queue.PlayerQueue.Enqueue(session.UserId);
        Dirty(hiveId, queue);

        _popup.PopupEntity(
            Loc.GetString("rmc-xeno-queue-joined", ("position", queue.PlayerQueue.Count)),
            session.AttachedEntity ?? EntityUid.Invalid,
            session.AttachedEntity ?? EntityUid.Invalid,
            PopupType.Medium);
        SendQueueStatusToAll((hiveId, hive), (hiveId, queue));
        return true;
    }

    protected override void OnLeaveLarvaQueueRequest(LeaveLarvaQueueRequest msg, EntitySessionEventArgs args)
    {
        var query = EntityQueryEnumerator<HiveComponent, LarvaQueueComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var queue))
        {
            if (!RemovePlayerFromQueue(queue, args.SenderSession.UserId))
                continue;

            Dirty(hiveId, queue);
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-queue-left"),
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                PopupType.Medium);
            SendQueueStatusToAll((hiveId, hive), (hiveId, queue));
            break;
        }
    }

    private bool RemovePlayerFromQueue(LarvaQueueComponent queue, NetUserId userId)
    {
        var tempQueue = new Queue<NetUserId>();
        var found = false;

        while (queue.PlayerQueue.Count > 0)
        {
            var queuedUserId = queue.PlayerQueue.Dequeue();
            if (queuedUserId == userId)
            {
                found = true;
                continue;
            }
            tempQueue.Enqueue(queuedUserId);
        }

        queue.PlayerQueue = tempQueue;
        return found;
    }

    protected override void SendQueueStatusToAll(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue)
    {
        var filter = Filter.Empty()
            .AddWhere(s => _rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(s.UserId) != PlayerGameStatus.JoinedGame);

        foreach (var session in filter.Recipients)
        {
            var position = GetQueuePosition(queue.Comp, session.UserId);
            var inQueue = position > 0;
            var totalAvailable = hive.Comp.BurrowedLarva + queue.Comp.PendingLarvae.Count;
            var statusEv = new LarvaQueueStatusEvent(
                position,
                queue.Comp.PlayerQueue.Count,
                totalAvailable,
                queue.Comp.PendingLarvae.Count,
                inQueue);
            RaiseNetworkEvent(statusEv, session);
        }
    }

    private int GetQueuePosition(LarvaQueueComponent queue, NetUserId userId)
    {
        var position = 1;
        foreach (var queuedUserId in queue.PlayerQueue)
        {
            if (queuedUserId == userId)
                return position;
            position++;
        }
        return 0;
    }

    protected override void OnJoinXenoBurrowedLarva(Entity<JoinXenoComponent> ent, ref JoinXenoBurrowedLarvaEvent args)
    {
        if (!TryGetEntity(args.Hive, out var hive) ||
            !TryComp(hive, out HiveComponent? hiveComp) ||
            !TryComp(ent, out ActorComponent? actor))
        {
            return;
        }

        if (_hive.JoinBurrowedLarva((hive.Value, hiveComp), actor.PlayerSession))
            return;

        if (TryComp<LarvaQueueComponent>(hive, out var queue) && queue.PendingLarvae.Count > 0)
        {
            var larva = queue.PendingLarvae.First();
            if (TryAssignLarva((hive.Value, hiveComp), larva, actor.PlayerSession))
            {
                queue.PendingLarvae.Remove(larva);
                _rmcGameTicker.PlayerJoinGame(actor.PlayerSession);
                Dirty(hive.Value, queue);
                return;
            }
        }

        _popup.PopupEntity(
            Loc.GetString("rmc-xeno-no-larvae-available"),
            ent.Owner,
            ent.Owner,
            PopupType.MediumCaution);
    }

    protected override void OnJoinBurrowedLarva(JoinBurrowedLarvaRequest msg, EntitySessionEventArgs args)
    {
        if (_rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(args.SenderSession.UserId) == PlayerGameStatus.JoinedGame)
            return;

        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive))
                continue;

            if (_hive.JoinBurrowedLarva((comp.Hive, hive), args.SenderSession))
            {
                _rmcGameTicker.PlayerJoinGame(args.SenderSession);
                break;
            }

            if (TryComp<LarvaQueueComponent>(comp.Hive, out var queue) && queue.PendingLarvae.Count > 0)
            {
                var larva = queue.PendingLarvae.First();
                if (TryAssignLarva((comp.Hive, hive), larva, args.SenderSession))
                {
                    queue.PendingLarvae.Remove(larva);
                    _rmcGameTicker.PlayerJoinGame(args.SenderSession);
                    Dirty(comp.Hive, queue);
                    break;
                }
            }
        }
    }

    private readonly record struct ExpiredPrompt(NetUserId UserId, NetEntity LarvaNetEntity);
}
