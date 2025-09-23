using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Hive;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class HiveCommand : ToolshedCommand
{
    [CommandImplementation("alldefault")]
    public void AllDefault([CommandInvocationContext] IInvocationContext ctx)
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
        var hiveSystem = EntityManager.System<SharedXenoHiveSystem>();
        while (xenos.MoveNext(out var uid, out _))
        {
            if (hiveSystem.HasHive(uid))
                continue;

            hiveSystem.SetHive(uid, firstHive);
            amount++;
        }

        var friendly = EntityManager.EntityQueryEnumerator<XenoFriendlyComponent>();
        while (friendly.MoveNext(out var uid, out _))
        {
            if (hiveSystem.HasHive(uid))
                continue;

            hiveSystem.SetHive(uid, firstHive);
            amount++;
        }

        ctx.WriteLine($"Set the hive of {amount} rogue xenos to {firstHive}.");
    }

    [CommandImplementation("set")]
    public EntityUid Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid xeno,
        [CommandArgument] Entity<HiveComponent> hive)
    {
        if (!HasComp<XenoComponent>(xeno) && !HasComp<XenoFriendlyComponent>(xeno))
        {
            ctx.WriteLine($"Entity {xeno} does not have {nameof(XenoComponent)} or {nameof(XenoFriendlyComponent)}");
            return xeno;
        }

        var hiveSystem = Sys<XenoHiveSystem>();
        hiveSystem.SetHive(xeno, hive);
        return xeno;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> xenos,
        [CommandArgument] Entity<HiveComponent> hive)
    {
        return xenos.Select(xeno => Set(ctx, xeno, hive));
    }
}
