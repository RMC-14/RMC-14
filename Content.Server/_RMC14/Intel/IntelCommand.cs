using Content.Server.Administration;
using Content.Shared._RMC14.Intel;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Intel;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class IntelCommand : ToolshedCommand
{
    [CommandImplementation("addpoints")]
    public async void AddPoints([CommandArgument] int points)
    {
        Sys<IntelSystem>().AddPoints(points);
    }

    [CommandImplementation("removepoints")]
    public async void RemovePoints([CommandArgument] int points)
    {
        Sys<IntelSystem>().AddPoints(-points);
    }
}
