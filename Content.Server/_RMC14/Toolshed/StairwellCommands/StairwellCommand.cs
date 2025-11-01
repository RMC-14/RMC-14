using Content.Server.Administration;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Toolshed.StairwellCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class StairwellCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Stairwell([PipedArgument] IEnumerable<EntityUid> input, int x, int y)
    {
        foreach (var entity in input)
        {
            if (!EntityManager.TryGetComponent<RMCTeleporterComponent>(entity, out var comp))
                continue;

#pragma warning disable RA0002
            comp.Adjust.X = x;
            comp.Adjust.Y = y;
#pragma warning restore RA0002
        }

        return;
    }
}
