using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Marines.Mutiny;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class MutinyCommand : ToolshedCommand
{
    [CommandImplementation("end"), AdminCommand(AdminFlags.Fun)]
    public void EndMutiny([CommandInvocationContext] IInvocationContext ctx)
    {
        if (!Sys<MutinyRuleSystem>().EndMutiny(out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-no-rule"));
        else
            ctx.WriteLine(Loc.GetString("rmc-mutiny-command-success"));
    }

    [CommandImplementation("list")]
    public void ListMutineers([CommandInvocationContext] IInvocationContext ctx)
    {
        foreach (var line in Sys<MutinyRuleSystem>().GetStatusLines())
            ctx.WriteLine(line);
    }

    [CommandImplementation("ismutineer")]
    public bool IsMutineer([PipedArgument] EntityUid marine)
    {
        return Sys<MutinyRuleSystem>().IsMutineer(marine);
    }

    [CommandImplementation("makemutineer"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeMutineer(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TryMakeMutineer(marine, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-invalid-member"));
        return marine;
    }

    [CommandImplementation("makemutineer"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeMutineer(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeMutineer(ctx, marine));
    }

    [CommandImplementation("removemutineer"), AdminCommand(AdminFlags.Fun)]
    public EntityUid RemoveMutineer(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TryRemoveMutineer(marine, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-not-mutineer"));
        return marine;
    }

    [CommandImplementation("removemutineer"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> RemoveMutineer(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => RemoveMutineer(ctx, marine));
    }

    [CommandImplementation("makemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeMutineerLeader(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TryAddLeader(marine, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-invalid-member"));
        return marine;
    }

    [CommandImplementation("makemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeMutineerLeader(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeMutineerLeader(ctx, marine));
    }

    [CommandImplementation("removemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public EntityUid RemoveMutineerLeader(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TryRemoveLeader(marine, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-not-leader"));
        return marine;
    }

    [CommandImplementation("removemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> RemoveMutineerLeader(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => RemoveMutineerLeader(ctx, marine));
    }

    [CommandImplementation("makeloyalist"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeLoyalist(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TrySetSide(marine, MutinySide.Loyalist, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-invalid-member"));
        return marine;
    }

    [CommandImplementation("makeloyalist"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeLoyalist(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeLoyalist(ctx, marine));
    }

    [CommandImplementation("makenoncombatant"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeNonCombatant(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        if (!Sys<MutinyRuleSystem>().TrySetSide(marine, MutinySide.NonCombatant, out var error))
            ctx.WriteLine(error ?? Loc.GetString("rmc-mutiny-error-invalid-member"));
        return marine;
    }

    [CommandImplementation("makenoncombatant"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeNonCombatant(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeNonCombatant(ctx, marine));
    }
}
