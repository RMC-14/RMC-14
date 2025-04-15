using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

public sealed class JoinXenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedRMCGameTickerSystem _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCGameTickerSystem _rmcGameTicker = default!;

    public int BurrowedLarva { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<JoinXenoComponent, MapInitEvent>(OnJoinXenoMapInit);
        SubscribeLocalEvent<JoinXenoComponent, JoinXenoActionEvent>(OnJoinXenoAction);
        SubscribeLocalEvent<JoinXenoComponent, JoinXenoBurrowedLarvaEvent>(OnJoinXenoBurrowedLarva);

        if (_net.IsClient)
        {
            SubscribeNetworkEvent<BurrowedLarvaStatusEvent>(OnBurrowedLarvaStatus);
        }
        else
        {
            SubscribeLocalEvent<RMCPlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
            SubscribeLocalEvent<BurrowedLarvaChangedEvent>(OnBurrowedLarvaChanged);
            SubscribeNetworkEvent<JoinBurrowedLarvaRequest>(OnJoinBurrowedLarva);
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        BurrowedLarva = 0;
        SendLarvaStatus(null);
    }

    private void OnJoinXenoMapInit(Entity<JoinXenoComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnJoinXenoAction(Entity<JoinXenoComponent> ent, ref JoinXenoActionEvent args)
    {
        var options = new List<DialogOption>();
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            if (hive.BurrowedLarva <= 0)
                continue;

            options.Add(new DialogOption("Burrowed Larva", new JoinXenoBurrowedLarvaEvent(GetNetEntity(hiveId))));
        }

        _dialog.OpenOptions(ent, "Join as Xeno", options, "Available Xenonids");
    }

    private void OnJoinXenoBurrowedLarva(Entity<JoinXenoComponent> ent, ref JoinXenoBurrowedLarvaEvent args)
    {
        if (!TryGetEntity(args.Hive, out var hive) ||
            !TryComp(hive, out HiveComponent? hiveComp) ||
            !TryComp(ent, out ActorComponent? actor))
        {
            return;
        }

        _hive.JoinBurrowedLarva((hive.Value, hiveComp), actor.PlayerSession);
    }

    private void OnBurrowedLarvaStatus(BurrowedLarvaStatusEvent ev)
    {
        BurrowedLarva = ev.Larva;
    }

    private void OnPlayerJoinedLobby(ref RMCPlayerJoinedLobbyEvent ev)
    {
        SendLarvaStatus(ev.Player);
    }

    private void OnBurrowedLarvaChanged(ref BurrowedLarvaChangedEvent ev)
    {
        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Hive != ev.Hive.Owner)
                continue;

            BurrowedLarva = ev.Hive.Comp.BurrowedLarva;
            SendLarvaStatus(null);
        }
    }

    private void OnJoinBurrowedLarva(JoinBurrowedLarvaRequest msg, EntitySessionEventArgs args)
    {
        if (!_rmcGameTicker.PlayerGameStatuses.TryGetValue(args.SenderSession.UserId, out var status) ||
            status == PlayerGameStatus.JoinedGame)
        {
            return;
        }

        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!TryComp(comp.Hive, out HiveComponent? hive) ||
                !_hive.JoinBurrowedLarva((comp.Hive, hive), args.SenderSession))
            {
                continue;
            }

            _gameTicker.PlayerJoinGame(args.SenderSession);
            break;
        }
    }

    private void SendLarvaStatus(ICommonSession? player)
    {
        if (_net.IsClient)
            return;

        var statusEv = new BurrowedLarvaStatusEvent(BurrowedLarva);
        if (player == null)
            RaiseNetworkEvent(statusEv);
        else
            RaiseNetworkEvent(statusEv, player);
    }

    public void ClientJoinLarva()
    {
        if (_net.IsServer)
            return;

        var ev = new JoinBurrowedLarvaRequest();
        RaiseNetworkEvent(ev);
    }
}
