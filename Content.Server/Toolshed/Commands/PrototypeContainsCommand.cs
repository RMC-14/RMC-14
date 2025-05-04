using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Toolshed.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class PrototypeContainsCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Prototyped(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] string prototype,
        [CommandInverted] bool inverted
    )
    {
        return input.Where(x => (MetaData(x).EntityPrototype?.ID.Contains(prototype) ?? false) ^ inverted);
    }
}
