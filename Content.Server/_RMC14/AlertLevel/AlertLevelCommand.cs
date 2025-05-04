using Content.Server.Administration;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.AlertLevel;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class AlertLevelCommand : ToolshedCommand
{
    [CommandImplementation("get")]
    public void Set(IInvocationContext context)
    {
        var level = Sys<RMCAlertLevelSystem>().Get();
        context.WriteLine($"The current alert level is {level}");
    }

    [CommandImplementation("set")]
    public void Set(IInvocationContext context, RMCAlertLevels level)
    {
        Sys<RMCAlertLevelSystem>().Set(level, ExecutingEntity(context));
        context.WriteLine($"Set the alert level to {level}");
    }
}
