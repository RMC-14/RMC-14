using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.Marines.Squads;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class SquadCommand : ToolshedCommand
{
    private SquadSystem? _squad;

    [CommandImplementation("get")]
    public EntityUid? Get([PipedArgument] EntityUid marine)
    {
        return EntityManager.GetComponentOrNull<SquadMemberComponent>(marine)?.Squad;
    }

    [CommandImplementation("getname")]
    public string GetName([PipedArgument] EntityUid marine)
    {
        if (Get(marine) is { } squad)
            return EntName(squad);

        return "No Squad";
    }

    [CommandImplementation("set")]
    public EntityUid Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument] SquadType squad)
    {
        _squad ??= GetSys<SquadSystem>();
        _squad.AssignSquad(marine, squad.Value, null);
        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] SquadType squad)
    {
        return marines.Select(marine => Set(ctx, marine, squad));
    }
}
