using Content.Server.Administration;
using Content.Shared._RMC14.Teleporter;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Toolshed.MappingCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class RenameCommand : ToolshedCommand
{
    private MetaDataSystem? _metaData;
    [CommandImplementation]
    public void StairwellProjector([PipedArgument] IEnumerable<EntityUid> input, string name)
    {
        _metaData ??= Sys<MetaDataSystem>();
        foreach (var entity in input)
        {
            _metaData.SetEntityName(entity, name);
        }
        return;
    }
}
