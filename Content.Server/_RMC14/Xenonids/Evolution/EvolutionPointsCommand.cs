using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Evolution;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class EvolutionPointsCommand : ToolshedCommand
{
    private XenoEvolutionSystem? _xenoEvolution;

    [CommandImplementation("get")]
    public int Get([PipedArgument] EntityUid xeno)
    {
        var points = EntityManager.GetComponentOrNull<XenoEvolutionComponent>(xeno)?.Points;
        return points?.Int() ?? 0;
    }

    [CommandImplementation("set")]
    public EntityUid Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid xeno,
        [CommandArgument] int points)
    {
        _xenoEvolution ??= GetSys<XenoEvolutionSystem>();
        if (!TryComp(xeno, out XenoEvolutionComponent? evolution))
            return xeno;

        _xenoEvolution.SetPoints((xeno, evolution), points);
        return xeno;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> xenos,
        [CommandArgument] int points)
    {
        return xenos.Select(xeno => Set(ctx, xeno, points));
    }

    [CommandImplementation("max")]
    public EntityUid Max(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid xeno)
    {
        if (!TryComp(xeno, out XenoEvolutionComponent? evolution) ||
            evolution.Points >= evolution.Max)
        {
            return xeno;
        }

        var max = evolution.Max;
        return Set(ctx, xeno, max.Int());
    }

    [CommandImplementation("max")]
    public IEnumerable<EntityUid> Max(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> xenos)
    {
        return xenos.Select(xeno => Max(ctx, xeno));
    }
}
