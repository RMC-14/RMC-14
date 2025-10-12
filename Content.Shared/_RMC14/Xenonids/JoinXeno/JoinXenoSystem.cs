using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

public abstract class SharedJoinXenoSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly DialogSystem _dialog = default!;
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] protected readonly INetManager _net = default!;
    [Dependency] protected readonly SharedRMCGameTickerSystem _rmcGameTicker = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedGameTicker _gameTicker = default!;
    [Dependency] protected readonly IConfigurationManager _config = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public int ClientBurrowedLarva { get; private set; }
    public LarvaQueueStatusEvent? ClientQueueStatus { get; private set; }

    protected TimeSpan _burrowedLarvaDeathTime;
    protected TimeSpan _burrowedLarvaDeathIgnoreTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<JoinXenoComponent, MapInitEvent>(OnJoinXenoMapInit);
        SubscribeLocalEvent<JoinXenoComponent, JoinXenoActionEvent>(OnJoinXenoAction);
        SubscribeLocalEvent<JoinXenoComponent, JoinXenoBurrowedLarvaEvent>(OnJoinXenoBurrowedLarva);
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);

        if (_net.IsClient)
        {
            SubscribeNetworkEvent<BurrowedLarvaStatusEvent>(OnBurrowedLarvaStatus);
            SubscribeNetworkEvent<LarvaQueueStatusEvent>(OnLarvaQueueStatus);
            SubscribeNetworkEvent<LarvaPromptEvent>(OnLarvaPrompt);
            SubscribeNetworkEvent<LarvaPromptCancelledEvent>(OnLarvaPromptCancelled);
        }
        else
        {
            SubscribeLocalEvent<RMCPlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
            SubscribeLocalEvent<BurrowedLarvaChangedEvent>(OnBurrowedLarvaChanged);
            SubscribeNetworkEvent<JoinBurrowedLarvaRequest>(OnJoinBurrowedLarva);
            SubscribeNetworkEvent<BurrowedLarvaStatusRequest>(OnBurrowedLarvaStatusRequest);
            SubscribeNetworkEvent<JoinLarvaQueueRequest>(OnJoinLarvaQueueRequest);
            SubscribeNetworkEvent<LeaveLarvaQueueRequest>(OnLeaveLarvaQueueRequest);
            SubscribeNetworkEvent<LarvaQueueStatusRequest>(OnLarvaQueueStatusRequest);
            SubscribeNetworkEvent<AcceptLarvaPromptRequest>(OnAcceptLarvaPrompt);
            SubscribeNetworkEvent<DeclineLarvaPromptRequest>(OnDeclineLarvaPrompt);
        }

        Subs.CVar(_config, RMCCVars.RMCLateJoinsBurrowedLarvaDeathTime, v => _burrowedLarvaDeathTime = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCLateJoinsBurrowedLarvaDeathTimeIgnoreBeforeMinutes, v => _burrowedLarvaDeathIgnoreTime = TimeSpan.FromMinutes(v), true);
    }

    protected virtual void OnJoinLarvaQueueRequest(JoinLarvaQueueRequest msg, EntitySessionEventArgs args) { }
    protected virtual void OnLeaveLarvaQueueRequest(LeaveLarvaQueueRequest msg, EntitySessionEventArgs args) { }
    protected virtual void OnAcceptLarvaPrompt(AcceptLarvaPromptRequest msg, EntitySessionEventArgs args) { }
    protected virtual void OnDeclineLarvaPrompt(DeclineLarvaPromptRequest msg, EntitySessionEventArgs args) { }

    protected virtual void OnLarvaPrompt(LarvaPromptEvent ev) { }
    protected virtual void OnLarvaPromptCancelled(LarvaPromptCancelledEvent ev) { }

    private void OnLarvaQueueStatusRequest(LarvaQueueStatusRequest msg, EntitySessionEventArgs args)
    {
        SendQueueStatus(args.SenderSession);
    }

    private void OnLarvaQueueStatus(LarvaQueueStatusEvent ev)
    {
        ClientQueueStatus = ev;
    }

    private void OnJoinLarvaQueue(Entity<JoinXenoComponent> ent, ref JoinLarvaQueueEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Hive, out var hive))
            return;

        if (!TryComp(hive, out HiveComponent? hiveComp))
            return;

        if (!TryComp(ent, out ActorComponent? actor))
            return;

        var queue = EnsureComp<LarvaQueueComponent>(hive.Value);

        if (queue.PlayerQueue.Contains(actor.PlayerSession.UserId))
        {
            var tempQueue = new Queue<NetUserId>();
            while (queue.PlayerQueue.Count > 0)
            {
                var userId = queue.PlayerQueue.Dequeue();
                if (userId != actor.PlayerSession.UserId)
                    tempQueue.Enqueue(userId);
            }
            queue.PlayerQueue = tempQueue;
            Dirty(hive.Value, queue);

            var leftMsg = Loc.GetString("rmc-xeno-queue-left");
            _popup.PopupEntity(leftMsg, ent.Owner, ent.Owner, PopupType.Medium);
            SendQueueStatusToAll((hive.Value, hiveComp), (hive.Value, queue));
            return;
        }

        if (queue.PlayerQueue.Count >= queue.MaxQueueSize)
        {
            var fullMsg = Loc.GetString("rmc-xeno-queue-full");
            _popup.PopupEntity(fullMsg, ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        queue.PlayerQueue.Enqueue(actor.PlayerSession.UserId);
        Dirty(hive.Value, queue);

        var joinedMsg = Loc.GetString("rmc-xeno-queue-joined", ("position", queue.PlayerQueue.Count));
        _popup.PopupEntity(joinedMsg, ent.Owner, ent.Owner, PopupType.Medium);

        if (_net.IsServer)
        {
            var scanEvent = new ScanExistingLarvaeEvent();
            RaiseLocalEvent(hive.Value, ref scanEvent);
        }

        SendQueueStatusToAll((hive.Value, hiveComp), (hive.Value, queue));
    }

    private void SendQueueStatus(ICommonSession? to)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ActiveGameRuleComponent, CMDistressSignalRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out _, out var comp, out _))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive) ||
                !TryComp(comp.Hive, out LarvaQueueComponent? queue))
                continue;

            if (to != null)
            {
                var position = GetQueuePosition(queue, to.UserId);
                var inQueue = position > 0;
                var totalAvailable = hive.BurrowedLarva + queue.PendingLarvae.Count;
                var statusEv = new LarvaQueueStatusEvent(position, queue.PlayerQueue.Count,
                    totalAvailable, queue.PendingLarvae.Count, inQueue);
                RaiseNetworkEvent(statusEv, to);
            }
            else
            {
                SendQueueStatusToAll((comp.Hive, hive), (comp.Hive, queue));
            }
            break;
        }
    }

    protected virtual void SendQueueStatusToAll(Entity<HiveComponent> hive, Entity<LarvaQueueComponent> queue) { }

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

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        ClientBurrowedLarva = 0;
        ClientQueueStatus = null;
        SendLarvaStatus(null);
    }

    private void OnJoinXenoMapInit(Entity<JoinXenoComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnJoinXenoAction(Entity<JoinXenoComponent> ent, ref JoinXenoActionEvent args)
    {
        if (_net.IsClient)
            return;

        var user = args.Performer;
        if (!CanJoinXeno(user))
            return;

        var options = new List<DialogOption>();
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            var totalAvailable = hive.BurrowedLarva;
            if (TryComp<LarvaQueueComponent>(hiveId, out var queue))
                totalAvailable += queue.PendingLarvae.Count;

            if (totalAvailable > 0)
            {
                options.Add(new DialogOption($"Immediate Larva ({totalAvailable} available)",
                    new JoinXenoBurrowedLarvaEvent(GetNetEntity(hiveId))));
            }

            var queueText = "Join Larva Queue";
            if (TryComp<LarvaQueueComponent>(hiveId, out var queueComp))
            {
                queueText += $" ({queueComp.PlayerQueue.Count} waiting)";
            }
            options.Add(new DialogOption(queueText, new JoinLarvaQueueEvent(GetNetEntity(hiveId))));
        }

        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-no-hives"), user, user, PopupType.MediumCaution);
            return;
        }

        _dialog.OpenOptions(ent, "Join as Xeno", options, "Available Options");
    }

    public bool CanJoinXeno(EntityUid user)
    {
        if (!TryComp<GhostComponent>(user, out var ghostComp))
            return false;

        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        var gameStatus = _rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(actor.PlayerSession.UserId);

        if (HasComp<JoinXenoCooldownIgnoreComponent>(user))
            return true;

        // If the game has been going on longer than the death ignore time, then check how long since the ghost has died
        if (_gameTicker.RoundDuration() > _burrowedLarvaDeathIgnoreTime)
        {
            var timeSinceDeath = _timing.CurTime.Subtract(ghostComp.TimeOfDeath);

            if (timeSinceDeath < _burrowedLarvaDeathTime)
            {
                var msg = Loc.GetString("rmc-xeno-ui-burrowed-need-time", ("seconds", _burrowedLarvaDeathTime.TotalSeconds - (int)timeSinceDeath.TotalSeconds));
                _popup.PopupEntity(msg, user, user, PopupType.MediumCaution);
                return false;
            }
        }

        return true;
    }

    protected virtual void OnJoinXenoBurrowedLarva(Entity<JoinXenoComponent> ent, ref JoinXenoBurrowedLarvaEvent args) { }

    private void OnBurrowedLarvaStatus(BurrowedLarvaStatusEvent ev)
    {
        ClientBurrowedLarva = ev.Larva;

        if (_net.IsServer)
            return;

        var changedEv = new BurrowedLarvaChangedEvent(ev.Larva);
        RaiseLocalEvent(ref changedEv);
    }

    private void OnPlayerJoinedLobby(ref RMCPlayerJoinedLobbyEvent ev)
    {
        SendLarvaStatus(ev.Player);
        SendQueueStatus(ev.Player);
    }

    private void OnBurrowedLarvaChanged(ref BurrowedLarvaChangedEvent ev)
    {
        SendLarvaStatus(null);
        SendQueueStatus(null);
    }

    protected virtual void OnJoinBurrowedLarva(JoinBurrowedLarvaRequest msg, EntitySessionEventArgs args) { }

    private void OnBurrowedLarvaStatusRequest(BurrowedLarvaStatusRequest msg, EntitySessionEventArgs args)
    {
        SendLarvaStatus(args.SenderSession);
    }

    private void SendLarvaStatus(ICommonSession? to)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ActiveGameRuleComponent, CMDistressSignalRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out _, out var comp, out _))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive))
                continue;

            var statusEv = new BurrowedLarvaStatusEvent(hive.BurrowedLarva);
            if (to != null)
            {
                RaiseNetworkEvent(statusEv, to);
                return;
            }

            var filter = Filter.Empty()
                .AddWhere(s =>
                    _rmcGameTicker.PlayerGameStatuses.GetValueOrDefault(s.UserId) != PlayerGameStatus.JoinedGame);
            RaiseNetworkEvent(statusEv, filter);
        }
    }

    public void RequestBurrowedLarvaStatus()
    {
        if (_net.IsServer)
            return;

        var ev = new BurrowedLarvaStatusRequest();
        RaiseNetworkEvent(ev);
    }

    public void ClientJoinLarva()
    {
        if (_net.IsServer)
            return;

        var ev = new JoinBurrowedLarvaRequest();
        RaiseNetworkEvent(ev);
    }

    public void ClientJoinLarvaQueue()
    {
        if (_net.IsServer)
            return;

        var ev = new JoinLarvaQueueRequest();
        RaiseNetworkEvent(ev);
    }

    public void ClientLeaveLarvaQueue()
    {
        if (_net.IsServer)
            return;

        var ev = new LeaveLarvaQueueRequest();
        RaiseNetworkEvent(ev);
    }

    public void RequestLarvaQueueStatus()
    {
        if (_net.IsServer)
            return;

        var ev = new LarvaQueueStatusRequest();
        RaiseNetworkEvent(ev);
    }

    public void AcceptLarvaPrompt(NetEntity larva)
    {
        if (_net.IsServer)
            return;

        var ev = new AcceptLarvaPromptRequest(larva);
        RaiseNetworkEvent(ev);
    }

    public void DeclineLarvaPrompt(NetEntity larva)
    {
        if (_net.IsServer)
            return;

        var ev = new DeclineLarvaPromptRequest(larva);
        RaiseNetworkEvent(ev);
    }
}
