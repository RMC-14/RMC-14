using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Players.RateLimiting;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Mentor;
using Content.Shared.Administration;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Mentor;

public sealed class MentorManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private const string RateLimitKey = "MentorHelp";
    private static readonly ProtoId<JobPrototype> MentorJob = "CMSeniorEnlistedAdvisor";

    private readonly List<ICommonSession> _activeMentors = new();
    private readonly Dictionary<NetUserId, bool> _mentors = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var userId = player.UserId;
        var isMentor = await _db.IsJobWhitelisted(player.UserId, MentorJob, cancel);

        if (!isMentor)
        {
            var dbData = await _db.GetAdminDataForAsync(userId, cancel);
            var flags = AdminFlags.None;
            if (dbData?.AdminRank?.Flags != null)
            {
                flags |= AdminFlagsHelper.NamesToFlags(dbData.AdminRank.Flags.Select(p => p.Flag));
            }

            if (dbData?.Flags != null)
            {
                flags |= AdminFlagsHelper.NamesToFlags(dbData.Flags.Select(p => p.Flag));
            }

            isMentor = flags.HasFlag(AdminFlags.MentorHelp);
        }

        _mentors[player.UserId] = isMentor;

        if (isMentor)
            _activeMentors.Add(player);
    }

    private void FinishLoad(ICommonSession player)
    {
        SendMentorStatus(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _mentors.Remove(player.UserId);
        _activeMentors.Remove(player);
    }

    private void OnMentorSendMessage(MentorSendMessageMsg message)
    {
        var destination = new NetUserId(message.To);
        if (!_player.TryGetSessionById(destination, out var destinationSession))
            return;

        var author = message.MsgChannel.UserId;
        if (!_player.TryGetSessionById(author, out var authorSession) ||
            !_activeMentors.Contains(authorSession))
        {
            return;
        }

        SendMentorMessage(
            destination,
            destinationSession.Name,
            author,
            authorSession.Name,
            message.Message,
            destinationSession.Channel
        );
    }

    private void OnMentorHelpMessage(MentorHelpMsg message)
    {
        if (!_player.TryGetSessionById(message.MsgChannel.UserId, out var author))
            return;

        SendMentorMessage(author.UserId, author.Name, author.UserId, author.Name, message.Message, message.MsgChannel);
    }

    private void OnDeMentor(DeMentorMsg message)
    {
        if (!_player.TryGetSessionById(message.MsgChannel.UserId, out var session) ||
            !_activeMentors.Contains(session))
        {
            return;
        }

        _activeMentors.Remove(session);
        SendMentorStatus(session);
    }

    private void OnReMentor(ReMentorMsg message)
    {
        if (!_player.TryGetSessionById(message.MsgChannel.UserId, out var session) ||
            !_mentors.TryGetValue(session.UserId, out var mentor) ||
            !mentor)
        {
            return;
        }

        _activeMentors.Add(session);
        SendMentorStatus(session);
    }

    private void SendMentorStatus(ICommonSession player)
    {
        var isMentor = _activeMentors.Contains(player);
        var canReMentor = _mentors.TryGetValue(player.UserId, out var mentor) && mentor;
        var msg = new MentorStatusMsg()
        {
            IsMentor = isMentor,
            CanReMentor = canReMentor,
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    private void SendMentorMessage(NetUserId destination, string destinationName, NetUserId author, string authorName, string message, INetChannel destinationChannel)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var recipients = new HashSet<INetChannel> { destinationChannel };
        var isMentor = false;
        foreach (var active in _activeMentors)
        {
            if (active.UserId == author)
                isMentor = true;

            recipients.Add(active.Channel);
        }

        var mentorMsg = new MentorMessage(
            destination,
            destinationName,
            author,
            authorName,
            message,
            DateTime.Now,
            isMentor
        );
        var messages = new List<MentorMessage> { mentorMsg };
        var receive = new MentorMessagesReceivedMsg { Messages = messages };
        foreach (var recipient in recipients)
        {
            try
            {
                _net.ServerSendMessage(receive, recipient);
            }
            catch (Exception e)
            {
                _log.RootSawmill.Error($"Error sending mentor help message:\n{e}");
            }
        }
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<MentorStatusMsg>();
        _net.RegisterNetMessage<MentorSendMessageMsg>(OnMentorSendMessage);
        _net.RegisterNetMessage<MentorHelpMsg>(OnMentorHelpMessage);
        _net.RegisterNetMessage<MentorMessagesReceivedMsg>();
        _net.RegisterNetMessage<DeMentorMsg>(OnDeMentor);
        _net.RegisterNetMessage<ReMentorMsg>(OnReMentor);
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
        _rateLimit.Register(
            RateLimitKey,
            new RateLimitRegistration(
                RMCCVars.RMCMentorHelpRateLimitPeriod,
                RMCCVars.RMCMentorHelpRateLimitCount,
                _ => { }
            )
        );
    }
}
