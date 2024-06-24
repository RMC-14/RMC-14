using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Hive;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class AllXenosSetDefaultHiveCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run([CommandInvocationContext] IInvocationContext ctx)
    {
        EntityUid firstHive = default;
        var hives = EntityManager.EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var uid, out _))
        {
            if (firstHive == default || uid.Id < firstHive.Id)
                firstHive = uid;
        }

        if (firstHive == default)
        {
            ctx.WriteLine("No hives were found.");
            return;
        }

        var amount = 0;
        var xenos = EntityManager.EntityQueryEnumerator<XenoComponent>();
        var xenoSystem = EntityManager.System<XenoSystem>();
        while (xenos.MoveNext(out var uid, out var xeno))
        {
            if (xeno.Hive != null)
                continue;

            xenoSystem.SetHive(uid, firstHive);
            amount++;
        }

        ctx.WriteLine($"Set the hive of {amount} xenos to {firstHive}.");
    }
}
