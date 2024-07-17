using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Rules;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class MarinesPerXenoCommand : ToolshedCommand
{
     private CMDistressSignalRuleSystem? _distressSignal;

    [CommandImplementation("get")]
    public void Get([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] string map)
    {
        _distressSignal ??= GetSys<CMDistressSignalRuleSystem>();

        var ratio = _distressSignal.MarinesPerXeno;
        if (!ratio.TryGetValue(map, out var value))
        {
            ctx.WriteLine($"No map found with name {map}. Valid maps:\n{string.Join(", ", ratio.Keys)}");
            return;
        }

        ctx.WriteLine($"Ratio for {map}: {value}");
    }

    [CommandImplementation("set")]
    public void Set([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] string map, [CommandArgument] float value)
    {
        _distressSignal ??= GetSys<CMDistressSignalRuleSystem>();

        var ratio = _distressSignal.MarinesPerXeno;
        if (!ratio.ContainsKey(map))
        {
            ctx.WriteLine($"No map found with name {map}. Valid maps:\n{string.Join(", ", ratio.Keys)}");
            return;
        }

        ratio[map] = value;
        ctx.WriteLine($"Set the marines per xeno for {map} to {value}");
    }
}
