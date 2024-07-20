using System.Linq;
using Content.Server._RMC14.Admin;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Marines;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class MarineCommand : ToolshedCommand
{
    [CommandImplementation("randomize")]
    public EntityUid Randomize([PipedArgument] EntityUid ent)
    {
        if (!HasComp<MarineComponent>(ent))
            return ent;

        var randomized = GetSys<RMCAdminSystem>().RandomizeMarine(ent);
        Del(ent);
        EnsureComp<MarineComponent>(randomized);
        return randomized;
    }

    [CommandImplementation("randomize")]
    public IEnumerable<EntityUid> Randomize([PipedArgument] IEnumerable<EntityUid> ents)
    {
        return ents.Select(Randomize);
    }
}
