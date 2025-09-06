using Content.Server._RMC14.Admin;
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
        if (!ent.Comp.HasPriorityPlayers)
            return;

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
        {
            ProcessLarvaPriority((args.Larva, priorityComp));
        }
    }

    private void OnLarvaPriorityCompleted(ref LarvaPriorityCompletedEvent args)
    {
        if (!TryComp<LarvaPriorityComponent>(args.Larva, out var priority))
            return;

        if (!args.WasAccepted)
        {
            AddLarvaToNormalQueue(args.Larva);
        }

        RemCompDeferred<LarvaPriorityComponent>(args.Larva);
    }

    private void ProcessLarvaPriority(Entity<LarvaPriorityComponent> larva)
    {
        var hive = _hive.GetHive(larva.Owner);
        if (hive == null)
            return;

        if (larva.Comp.OriginalParasiteUserId != null && !larva.Comp.ParasiteOffered)
        {
            if (TryOfferLarvaToPriorityPlayer(larva, hive.Value, larva.Comp.OriginalParasiteUserId.Value, "original parasite"))
            {
                larva.Comp.ParasiteOffered = true;
                Dirty(larva.Owner, larva.Comp);
                return;
            }
            else
            {
                larva.Comp.ParasiteOffered = true;
                Dirty(larva.Owner, larva.Comp);
            }
        }

        if (larva.Comp.BurstVictimUserId != null && !larva.Comp.VictimOffered)
        {
            if (TryOfferLarvaToPriorityPlayer(larva, hive.Value, larva.Comp.BurstVictimUserId.Value, "burst victim"))
            {
                larva.Comp.VictimOffered = true;
                Dirty(larva.Owner, larva.Comp);
                return;
            }
            else
            {
                larva.Comp.VictimOffered = true;
                Dirty(larva.Owner, larva.Comp);
            }
        }

        AddLarvaToNormalQueue(larva.Owner);
        RemCompDeferred<LarvaPriorityComponent>(larva.Owner);
    }

    private bool TryOfferLarvaToPriorityPlayer(Entity<LarvaPriorityComponent> larva, Entity<HiveComponent> hive, NetUserId userId, string playerType)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session))
            return false;

        if (!CanStayInQueue(session))
            return false;

        var promptComp = EnsureComp<LarvaQueuePromptComponent>(hive.Owner);

        if (promptComp.PendingPrompts.ContainsKey(userId))
            return false;

        var expiryTime = _timing.CurTime + promptComp.PromptTimeout;
        var larvaNetEntity = GetNetEntity(larva.Owner);
        promptComp.PendingPrompts[userId] = (larvaNetEntity, expiryTime);

        var promptEvent = new LarvaPromptEvent(larvaNetEntity, GetNetEntity(hive.Owner), expiryTime);
        RaiseNetworkEvent(promptEvent, session);

        Dirty(hive.Owner, promptComp);
        return true;
    }

    private void AddLarvaToNormalQueue(EntityUid larva)
    {
        var hive = _hive.GetHive(larva);
        if (hive == null)
            return;

        if (!TryComp<LarvaQueueComponent>(hive.Value.Owner, out var queue))
        {
            queue = EnsureComp<LarvaQueueComponent>(hive.Value.Owner);
        }

        if (!queue.PendingLarvae.Contains(larva))
        {
            queue.PendingLarvae.Add(larva);
            Dirty(hive.Value.Owner, queue);

            if (queue.PlayerQueue.Count > 0)
            {
                ProcessLarvaQueue(hive.Value, (hive.Value.Owner, queue));
            }
        }
    }

    private bool CanStayInQueue(ICommonSession session)
    {
        if (session.AttachedEntity == null)
            return false;

        var entity = session.AttachedEntity.Value;

        if (HasComp<GhostComponent>(entity))
            return true;

        if (HasComp<XenoParasiteComponent>(entity))
            return true;

        if (TryComp<XenoComponent>(entity, out var xeno) &&
            xeno.Role.Id == "CMXenoLesserDrone")
            return true;

        if (HasComp<VictimBurstComponent>(entity))
            return true;

        if (HasComp<VictimInfectedComponent>(entity))
            return true;

        return false;
    }

    private void CleanupInvalidQueuePlayers(Entity<LarvaQueueComponent> queue)
    {
        var tempQueue = new Queue<NetUserId>();
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
                tempQueue.Enqueue(userId);
            }
            else
            {
                removedCount++;
                if (session.AttachedEntity != null)
                {
                    var msg = Loc.GetString("rmc-xeno-queue-removed-invalid-state");
                    _popup.PopupEntity(msg, session.AttachedEntity.Value,
                        session.AttachedEntity.Value, PopupType.MediumCaution);
                }
            }
        }

        queue.Comp.PlayerQueue = tempQueue;

        if (removedCount > 0)
        {
            Dirty(queue);
        }
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

        var expiredPrompts = new List<NetUserId>();

        foreach (var (userId, (larvaNetEntity, expiryTime)) in promptComp.PendingPrompts)
        {
            if (currentTime < expiryTime)
                continue;

            expiredPrompts.Add(userId);

            if (!TryGetEntity(larvaNetEntity, out var larva))
                continue;

            if (TryComp<LarvaPriorityComponent>(larva.Value, out var priorityComp))
            {
                ProcessLarvaPriority((larva.Value, priorityComp));
            }
            else
            {
                if (!TerminatingOrDeleted(larva.Value) && !_mobState.IsDead(larva.Value))
                {
                    queue.Comp.PendingLarvae.Add(larva.Value);
                }
                queue.Comp.PlayerQueue.Enqueue(userId);
            }

            if (_playerManager.TryGetSessionById(userId, out var session))
            {
                var cancelEv = new LarvaPromptCancelledEvent(larvaNetEntity, "timeout");
                RaiseNetworkEvent(cancelEv, session);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-larva-prompt-timeout"),
                    session.AttachedEntity ?? EntityUid.Invalid,
                    session.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);
            }
        }

        foreach (var expiredUserId in expiredPrompts)
        {
            promptComp.PendingPrompts.Remove(expiredUserId);
        }

        if (expiredPrompts.Count > 0)
        {
            Dirty(hive.Owner, promptComp);
            Dirty(queue.Owner, queue.Comp);
            SendQueueStatusToAll(hive, queue);
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
        var foundCount = 0;
        var infectedQuery = EntityQueryEnumerator<VictimInfectedComponent>();

        while (infectedQuery.MoveNext(out var hostId, out var infected))
        {
            if (infected.SpawnedLarva == null)
                continue;

            var larva = infected.SpawnedLarva.Value;
            var larvaHive = _hive.GetHive(larva);

            if (larvaHive?.Owner != queue.Owner)
                continue;

            if (!infected.IsBursting && !queue.Comp.PendingLarvae.Contains(larva) && !HasComp<LarvaPriorityComponent>(larva))
            {
                queue.Comp.PendingLarvae.Add(larva);
                foundCount++;
            }
        }

        if (foundCount > 0)
        {
            Dirty(queue);

            if (queue.Comp.PlayerQueue.Count > 0 && TryComp<HiveComponent>(queue.Owner, out var hive))
            {
                ProcessLarvaQueue((queue.Owner, hive), queue);
            }
        }
    }

    private void OnLarvaReadyToBurst(ref LarvaReadyToBurstEvent args)
    {
        var hive = _hive.GetHive(args.Larva);
        if (hive == null)
            return;

        if (!TryComp<LarvaQueueComponent>(hive.Value.Owner, out var queue))
        {
            queue = EnsureComp<LarvaQueueComponent>(hive.Value.Owner);
        }

        if (HasComp<LarvaPriorityComponent>(args.Larva))
        {
            SendQueueStatusToAll(hive.Value, (hive.Value.Owner, queue));
            return;
        }

        var wasInPending = queue.PendingLarvae.Contains(args.Larva);
        queue.PendingLarvae.Add(args.Larva);
        Dirty(hive.Value.Owner, queue);

        if (queue.PlayerQueue.Count > 0)
        {
            ProcessLarvaQueue(hive.Value, (hive.Value.Owner, queue));
        }

        SendQueueStatusToAll(hive.Value, (hive.Value.Owner, queue));
    }

    private void OnBurstLarvaConsumed(ref BurstLarvaConsumedEvent args)
    {
        var query = EntityQueryEnumerator<LarvaQueueComponent>();
        while (query.MoveNext(out var queueId, out var queue))
        {
            if (queue.PendingLarvae.Remove(args.Larva))
            {
                Dirty(queueId, queue);

                if (TryComp<HiveComponent>(queueId, out var hive))
                    SendQueueStatusToAll((queueId, hive), (queueId, queue));
                break;
            }
        }
    }

    private void ProcessLarvaQueue(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue)
    {
        CleanupStaleLarvae(queue);

        if (queue.Comp.PlayerQueue.Count == 0)
            return;

        var normalLarvaeCount = 0;
        var priorityLarvae = new List<EntityUid>();

        foreach (var larva in queue.Comp.PendingLarvae)
        {
            if (TryComp<LarvaPriorityComponent>(larva, out var priority) && priority.HasPriorityPlayers)
            {
                priorityLarvae.Add(larva);
            }
            else
            {
                normalLarvaeCount++;
            }
        }

        var totalAvailable = hive.Comp.BurrowedLarva + normalLarvaeCount;

        if (totalAvailable <= 0)
            return;

        var promptComp = EnsureComp<LarvaQueuePromptComponent>(hive.Owner);
        var processed = 0;

        while (processed < totalAvailable && queue.Comp.PlayerQueue.Count > 0)
        {
            var userId = queue.Comp.PlayerQueue.Dequeue();

            if (!_playerManager.TryGetSessionById(userId, out var session))
            {
                processed++;
                continue;
            }

            if (!CanStayInQueue(session))
            {
                processed++;
                continue;
            }

            if (promptComp.PendingPrompts.ContainsKey(userId))
            {
                queue.Comp.PlayerQueue.Enqueue(userId);
                continue;
            }

            EntityUid? availableLarva = null;

            foreach (var larva in queue.Comp.PendingLarvae)
            {
                if (!TryComp<LarvaPriorityComponent>(larva, out var priority) || !priority.HasPriorityPlayers)
                {
                    availableLarva = larva;
                    queue.Comp.PendingLarvae.Remove(larva);
                    break;
                }
            }

            if (availableLarva == null && hive.Comp.BurrowedLarva > 0)
            {
                if (TrySpawnBurrowedLarvaForPrompt(hive, out var burrowedLarva))
                {
                    availableLarva = burrowedLarva;
                }
            }

            if (availableLarva == null)
            {
                queue.Comp.PlayerQueue.Enqueue(userId);
                break;
            }

            var expiryTime = _timing.CurTime + promptComp.PromptTimeout;
            var larvaNetEntity = GetNetEntity(availableLarva.Value);
            promptComp.PendingPrompts[userId] = (larvaNetEntity, expiryTime);

            var promptEvent = new LarvaPromptEvent(larvaNetEntity, GetNetEntity(hive.Owner), expiryTime);
            RaiseNetworkEvent(promptEvent, session);

            processed++;
        }

        if (processed > 0)
        {
            Dirty(hive.Owner, promptComp);
            Dirty(queue);
            SendQueueStatusToAll(hive, queue);
        }
    }

    private bool TrySpawnBurrowedLarvaForPrompt(Entity<HiveComponent> hive, out EntityUid larva)
    {
        larva = default;

        if (hive.Comp.BurrowedLarva <= 0)
            return false;

        EntityUid spawnedLarva = default;

        bool TrySpawnAt<T>() where T : Component
        {
            var candidates = EntityQueryEnumerator<T, HiveMemberComponent>();
            while (candidates.MoveNext(out var uid, out _, out var member))
            {
                if (member.Hive != hive.Owner)
                    continue;

                if (_mobState.IsDead(uid))
                    continue;

                var position = Transform(uid).Coordinates;
                spawnedLarva = Spawn(hive.Comp.BurrowedLarvaId, position);
                return true;
            }
            return false;
        }

        if (!TrySpawnAt<HiveCoreComponent>() &&
            !TrySpawnAt<XenoEvolutionGranterComponent>() &&
            !TrySpawnAt<XenoComponent>())
        {
            return false;
        }

        if (spawnedLarva == default)
            return false;

        larva = spawnedLarva;

        _hive.IncreaseBurrowedLarva(hive, -1);

        _xeno.MakeXeno(larva);
        _hive.SetHive(larva, hive.Owner);

        return true;
    }

    private void CleanupStaleLarvae(Entity<LarvaQueueComponent> queue)
    {
        var toRemove = new List<EntityUid>();

        foreach (var larva in queue.Comp.PendingLarvae)
        {
            if (TerminatingOrDeleted(larva) || _mobState.IsDead(larva))
            {
                toRemove.Add(larva);
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (var larva in toRemove)
            {
                queue.Comp.PendingLarvae.Remove(larva);
            }
            Dirty(queue);
        }
    }

    private bool TryAssignLarva(Entity<HiveComponent> hive, EntityUid larva, ICommonSession session)
    {
        if (TerminatingOrDeleted(larva))
            return false;

        if (_mobState.IsDead(larva))
            return false;

        if (!TryComp<MetaDataComponent>(larva, out var metaData))
            return false;

        var newMind = _mind.CreateMind(session.UserId, metaData.EntityName);
        _mind.TransferTo(newMind, larva, ghostCheckOverride: true);

        if (TryComp<BursterComponent>(larva, out var burster))
        {
            if (TryComp<VictimInfectedComponent>(burster.BurstFrom, out var infected))
            {
                _parasiteSystem.TryStartBurst((burster.BurstFrom, infected));
            }
        }

        var consumedEv = new BurstLarvaConsumedEvent(larva);
        RaiseLocalEvent(ref consumedEv);

        return true;
    }

    protected override void OnAcceptLarvaPrompt(AcceptLarvaPromptRequest msg, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(msg.Larva, out var larva))
            return;

        var query = EntityQueryEnumerator<HiveComponent, LarvaQueuePromptComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var promptComp))
        {
            if (!promptComp.PendingPrompts.TryGetValue(args.SenderSession.UserId, out var promptData))
                continue;

            var (promptLarvaNetEntity, expiryTime) = promptData;
            if (promptLarvaNetEntity != msg.Larva)
                continue;

            if (_timing.CurTime >= expiryTime)
            {
                var timeoutMsg = Loc.GetString("rmc-xeno-larva-prompt-expired");
                _popup.PopupEntity(timeoutMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);
                return;
            }

            promptComp.PendingPrompts.Remove(args.SenderSession.UserId);
            Dirty(hiveId, promptComp);

            if (TryAssignLarva((hiveId, hive), larva.Value, args.SenderSession))
            {
                _rmcGameTicker.PlayerJoinGame(args.SenderSession);
                var successMessage = Loc.GetString("rmc-xeno-larva-accepted");
                _popup.PopupEntity(successMessage, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.Medium);

                if (TryComp<LarvaPriorityComponent>(larva.Value, out _))
                {
                    var completedEv = new LarvaPriorityCompletedEvent(larva.Value, true, args.SenderSession.UserId);
                    RaiseLocalEvent(ref completedEv);
                }
            }
            else
            {
                var failureMessage = Loc.GetString("rmc-xeno-larva-assignment-failed");
                _popup.PopupEntity(failureMessage, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);

                if (TryComp<LarvaPriorityComponent>(larva.Value, out var priority))
                {
                    ProcessLarvaPriority((larva.Value, priority));
                }
                else if (TryComp<LarvaQueueComponent>(hiveId, out var queue))
                {
                    if (!TerminatingOrDeleted(larva.Value) && !_mobState.IsDead(larva.Value))
                    {
                        queue.PendingLarvae.Add(larva.Value);
                        Dirty(hiveId, queue);
                    }
                }
            }

            if (TryComp<LarvaQueueComponent>(hiveId, out var queueComp))
                SendQueueStatusToAll((hiveId, hive), (hiveId, queueComp));

            return;
        }
    }

    protected override void OnDeclineLarvaPrompt(DeclineLarvaPromptRequest msg, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(msg.Larva, out var larva))
            return;

        var query = EntityQueryEnumerator<HiveComponent, LarvaQueuePromptComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var promptComp))
        {
            if (!promptComp.PendingPrompts.TryGetValue(args.SenderSession.UserId, out var promptData))
                continue;

            var (promptLarvaNetEntity, _) = promptData;
            if (promptLarvaNetEntity != msg.Larva)
                continue;

            promptComp.PendingPrompts.Remove(args.SenderSession.UserId);
            Dirty(hiveId, promptComp);

            var declineMsg = Loc.GetString("rmc-xeno-larva-declined");
            _popup.PopupEntity(declineMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.Medium);

            if (TryComp<LarvaPriorityComponent>(larva.Value, out var priority))
            {
                ProcessLarvaPriority((larva.Value, priority));
            }
            else if (TryComp<LarvaQueueComponent>(hiveId, out var queue))
            {
                if (!TerminatingOrDeleted(larva.Value) && !_mobState.IsDead(larva.Value))
                {
                    queue.PendingLarvae.Add(larva.Value);
                }

                queue.PlayerQueue.Enqueue(args.SenderSession.UserId);
                Dirty(hiveId, queue);

                SendQueueStatusToAll((hiveId, hive), (hiveId, queue));
            }

            return;
        }
    }

    protected override void OnJoinLarvaQueueRequest(JoinLarvaQueueRequest msg, EntitySessionEventArgs args)
    {
        if (_rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(args.SenderSession.UserId) == PlayerGameStatus.JoinedGame)
            return;

        if (!CanStayInQueue(args.SenderSession))
        {
            var invalidMsg = Loc.GetString("rmc-xeno-queue-invalid-state");
            _popup.PopupEntity(invalidMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);
            return;
        }

        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive))
                continue;

            var queue = EnsureComp<LarvaQueueComponent>(comp.Hive);

            if (queue.PlayerQueue.Contains(args.SenderSession.UserId))
            {
                var alreadyInMsg = Loc.GetString("rmc-xeno-queue-already-in");
                _popup.PopupEntity(alreadyInMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);
                return;
            }

            if (queue.PlayerQueue.Count >= queue.MaxQueueSize)
            {
                var fullMsg = Loc.GetString("rmc-xeno-queue-full");
                _popup.PopupEntity(fullMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.MediumCaution);
                return;
            }

            queue.PlayerQueue.Enqueue(args.SenderSession.UserId);
            Dirty(comp.Hive, queue);

            var joinedMsg = Loc.GetString("rmc-xeno-queue-joined", ("position", queue.PlayerQueue.Count));
            _popup.PopupEntity(joinedMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.Medium);

            SendQueueStatusToAll((comp.Hive, hive), (comp.Hive, queue));
            break;
        }
    }

    protected override void OnLeaveLarvaQueueRequest(LeaveLarvaQueueRequest msg, EntitySessionEventArgs args)
    {
        var query = EntityQueryEnumerator<HiveComponent, LarvaQueueComponent>();
        while (query.MoveNext(out var hiveId, out var hive, out var queue))
        {
            var tempQueue = new Queue<NetUserId>();
            var found = false;

            while (queue.PlayerQueue.Count > 0)
            {
                var userId = queue.PlayerQueue.Dequeue();
                if (userId == args.SenderSession.UserId)
                {
                    found = true;
                    continue;
                }
                tempQueue.Enqueue(userId);
            }

            queue.PlayerQueue = tempQueue;

            if (found)
            {
                Dirty(hiveId, queue);
                var leftMsg = Loc.GetString("rmc-xeno-queue-left");
                _popup.PopupEntity(leftMsg, args.SenderSession.AttachedEntity ?? EntityUid.Invalid,
                    args.SenderSession.AttachedEntity ?? EntityUid.Invalid, PopupType.Medium);
                SendQueueStatusToAll((hiveId, hive), (hiveId, queue));
                break;
            }
        }
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
            var statusEv = new LarvaQueueStatusEvent(position, queue.Comp.PlayerQueue.Count,
                totalAvailable, queue.Comp.PendingLarvae.Count, inQueue);
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

        _popup.PopupEntity(Loc.GetString("rmc-xeno-no-larvae-available"), ent.Owner, ent.Owner, PopupType.MediumCaution);
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
}
