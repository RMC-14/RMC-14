using Content.Server.Administration;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Toolshed.StairwellCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class StairwellProjectorCommand : ToolshedCommand
{
    [CommandImplementation]
    public void StairwellProjector([PipedArgument] IEnumerable<EntityUid> input, string id)
    {
        foreach (var entity in input)
        {
            var comp = EntityManager.GetComponent<RMCTeleporterViewerComponent>(entity);

#pragma warning disable RA0002
            comp.Id = id;
#pragma warning restore RA0002
        }

        return;
    }
}
