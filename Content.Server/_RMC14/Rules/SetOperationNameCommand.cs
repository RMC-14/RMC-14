using Content.Server._RMC14.Rules;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules;

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class SetOperationNameCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run(IInvocationContext ctx, string customname)
    {
        if (Sys<GameTicker>().RunLevel != GameRunLevel.PreRoundLobby)
        {
            ctx.WriteLine("This command can only be run in the lobby!");
            return;
        }

        var distressSignal = Sys<CMDistressSignalRuleSystem>();
        distressSignal.SetCustomOperationName(customname);

        ctx.WriteLine($"Next round's operation name set to {customname}!");
    }
}
