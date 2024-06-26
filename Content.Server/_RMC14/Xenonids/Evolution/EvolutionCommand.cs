using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._RMC14.Xenonids.Evolution;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class EvolutionCommand : ToolshedCommand
{
    private XenoEvolutionSystem? _xenoEvolution;

    [CommandImplementation("getpoints")]
    public int GetPoints([PipedArgument] EntityUid xeno)
    {
        var points = EntityManager.GetComponentOrNull<XenoEvolutionComponent>(xeno)?.Points;
        return points?.Int() ?? 0;
    }

    [CommandImplementation("setpoints")]
    public EntityUid SetPoints(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid xeno,
        [CommandArgument] ValueRef<int> points)
    {
        _xenoEvolution ??= GetSys<XenoEvolutionSystem>();
        if (!TryComp(xeno, out XenoEvolutionComponent? evolution))
            return xeno;

        _xenoEvolution.SetPoints((xeno, evolution), points.Evaluate(ctx));
        return xeno;
    }

    [CommandImplementation("setpoints")]
    public IEnumerable<EntityUid> SetPoints(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> xenos,
        [CommandArgument] ValueRef<int> points)
    {
        return xenos.Select(xeno => SetPoints(ctx, xeno, points));
    }

    [CommandImplementation("maxpoints")]
    public EntityUid MaxPoints(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid xeno)
    {
        if (!TryComp(xeno, out XenoEvolutionComponent? evolution))
            return xeno;

        var max = evolution.Max;
        return SetPoints(ctx, xeno, new ValueRef<int>(max.Int()));
    }

    [CommandImplementation("maxpoints")]
    public IEnumerable<EntityUid> MaxPoints(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> xenos)
    {
        return xenos.Select(xeno => MaxPoints(ctx, xeno));
    }
}
