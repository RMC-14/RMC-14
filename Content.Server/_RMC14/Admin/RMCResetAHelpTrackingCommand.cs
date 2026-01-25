using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class RMCResetAHelpTrackingCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run()
    {
        var bwoink = Sys<BwoinkSystem>();
        bwoink.RMCRelayMessages.Clear();
    }
}
