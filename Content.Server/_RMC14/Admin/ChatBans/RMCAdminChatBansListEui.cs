using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin.ChatBans;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;

namespace Content.Server._RMC14.Admin.ChatBans;

public sealed class RMCAdminChatBansListEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly RMCChatBansManager _rmcChatBans = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    private readonly List<ChatBan> _bans = new();
    public NetUserId Target;

    public RMCAdminChatBansListEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        LoadFromDb();
    }

    public override EuiStateBase GetNewState()
    {
        return new RMCAdminChatBanListState(_bans);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_admin.HasAdminFlag(Player, AdminFlags.Ban))
            return;

        switch (msg)
        {
            case RMCAdminChatBanListPardonMsg pardon:
                _rmcChatBans.TryPardonChatBan(pardon.Id, Player.UserId);
                break;
        }
    }

    private async void LoadFromDb()
    {
        try
        {
            var bans = await _rmcChatBans.GetAllChatBans(Target);
            _task.RunOnMainThread(() =>
            {
                _bans.Clear();
                _bans.AddRange(bans);
                StateDirty();
            });
        }
        catch (Exception e)
        {
            _logManager.GetSawmill("rmc_chat_bans").Error($"Error loading chat bans from database:\n{e}");
        }
    }
}
