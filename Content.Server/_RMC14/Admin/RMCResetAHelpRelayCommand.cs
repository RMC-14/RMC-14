using System.Threading.Tasks;
using Content.Server._RMC14.Discord;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class RMCResetAHelpRelayCommand : ToolshedCommand
{
    [CommandImplementation]
    public async Task Run([CommandInvocationContext] IInvocationContext ctx)
    {
        var discord = IoCManager.Resolve<RMCDiscordManager>();
        await discord.Restart();
        ctx.WriteLine("Restarted the AHelp and MHelp relays.");
    }
}
