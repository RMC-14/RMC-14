using System.Collections;
using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Maturing;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class XenoMatureCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid Mature([PipedArgument] EntityUid ent)
    {
        if (!TryComp(ent, out XenoMaturingComponent? maturing))
            return ent;

        GetSys<XenoMaturingSystem>().Mature((ent, maturing));
        return ent;
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Mature([PipedArgument] IEnumerable<EntityUid> ents)
    {
        return ents.Select(Mature);
    }
}
