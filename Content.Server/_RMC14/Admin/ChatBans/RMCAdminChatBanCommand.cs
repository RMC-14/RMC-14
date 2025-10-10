using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin.ChatBans;

[AdminCommand(AdminFlags.Ban)]
public sealed class RMCAdminChatBanCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "rmcadminchatban";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            return;

        _eui.OpenEui(new RMCAdminChatBansEui(), shell.Player);
    }
}
