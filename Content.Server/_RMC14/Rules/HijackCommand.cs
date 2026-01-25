using Content.Server.Administration;
using Content.Shared._RMC14.Dropship;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Rules;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class HijackCommand : ToolshedCommand
{
    [CommandImplementation("trigger")]
    public void Trigger([CommandInvocationContext] IInvocationContext ctx)
    {
        if (ExecutingEntity(ctx) is not { } player ||
            Transform(player).MapUid is not { } map)
        {
            ctx.WriteLine("This command can only be run by a player entity!");
            return;
        }

        var ev = new DropshipHijackLandedEvent(map);
        EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
