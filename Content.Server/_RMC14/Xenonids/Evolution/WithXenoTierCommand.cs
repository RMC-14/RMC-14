using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Evolution;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
public sealed class WithXenoTierCommand : ToolshedCommand
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> With(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] int tier,
        [CommandInverted] bool inverted)
    {
        return input.Where(x => (EntityManager.GetComponentOrNull<XenoComponent>(x)?.Tier == tier) ^ inverted);
    }

    [CommandImplementation]
    public IEnumerable<EntityPrototype> With(
        [PipedArgument] IEnumerable<EntityPrototype> input,
        [CommandArgument] int tier,
        [CommandInverted] bool inverted)
    {
        return input.Where(x => (x.TryGetComponent(out XenoComponent? xeno, _compFactory) && xeno.Tier == tier) ^ inverted);
    }
}
