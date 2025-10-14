using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

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
        [CommandArgument] Entity<SquadTeamComponent> squad)
    {
        if (!HasComp<MarineComponent>(marine))
            return marine;

        _squad ??= GetSys<SquadSystem>();
        _squad.AssignSquad(marine, (squad, squad), null);
        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] Entity<SquadTeamComponent> squad)
    {
        return marines.Select(marine => Set(ctx, marine, squad));
    }

    [CommandImplementation("with")]
    public IEnumerable<EntityUid> With(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] Entity<SquadTeamComponent> squad)
    {
        return marines.Where(marine => TryComp(marine, out SquadMemberComponent? member) && member.Squad == squad);
    }

    [CommandImplementation("refresh")]
    public EntityUid Refresh(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] Entity<SquadTeamComponent> squad)
    {
        _squad ??= GetSys<SquadSystem>();
        _squad.RefreshSquad((squad, squad));
        return squad;
    }
}
