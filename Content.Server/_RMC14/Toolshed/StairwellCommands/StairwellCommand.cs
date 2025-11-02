using Content.Server.Administration;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
            var transform = EntityManager.GetComponent<TransformComponent>(entity);
            var comp = EntityManager.GetComponent<RMCTeleporterComponent>(entity);

#pragma warning disable RA0002
            comp.Adjust.X = x;
            comp.Adjust.Y = y;
#pragma warning restore RA0002
        }

        return;
    }
}
