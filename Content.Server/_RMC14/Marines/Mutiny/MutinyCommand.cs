using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Marines.Mutiny;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class MutinyCommand : ToolshedCommand
{
    [CommandImplementation("end"), AdminCommand(AdminFlags.Fun)]
    public void EndMutiny([CommandInvocationContext] IInvocationContext ctx)
    {
        var mutineers = EntityManager.EntityQueryEnumerator<MutineerComponent>();

        int numMutineers = 0;
        int numNonPlayerMutineers = 0;

        while (mutineers.MoveNext(out var mutineer, out var comp))
        {
            RemComp<MutineerComponent>(mutineer);
            numMutineers++;
            if (!HasComp<ActorComponent>(mutineer))
                numNonPlayerMutineers++;
        }

        ctx.WriteLine($"Set {numMutineers} mutineer players to be non-mutineers, of which {numNonPlayerMutineers} had no player attached.");
    }

    [CommandImplementation("list")]
    public void ListMutineers([CommandInvocationContext] IInvocationContext ctx)
    {
        var mutineers = EntityManager.EntityQueryEnumerator<MutineerComponent>();

        while (mutineers.MoveNext(out var mutineer, out var comp))
        {
            if (TryComp<ActorComponent>(mutineer, out var actorComponent))
            {
                ctx.WriteLine($"- Player {actorComponent.PlayerSession.Name} in entity {mutineer}");
            }
            else
            {
                ctx.WriteLine($"- Entity {mutineer}, with no player attached.");
            }
        }
    }

    [CommandImplementation("ismutineer")]
    public String IsMutineer([PipedArgument] EntityUid marine)
    {
        return HasComp<MutineerComponent>(marine) ? "Yes" : "No";
    }

    [CommandImplementation("makemutineer"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeMutineer([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        EnsureComp<MutineerComponent>(marine);

        return marine;
    }

    [CommandImplementation("makemutineer"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeMutineer([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeMutineer(ctx, marine));
    }

    [CommandImplementation("removemutineer"), AdminCommand(AdminFlags.Fun)]
    public EntityUid RemoveMutineer([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        RemComp<MutineerComponent>(marine);

        return marine;
    }

    [CommandImplementation("removemutineer"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> RemoveMutineer([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => RemoveMutineer(ctx, marine));
    }


    [CommandImplementation("makemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public EntityUid MakeMutineerLeader([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        EnsureComp<MutineerLeaderComponent>(marine);
        return marine;
    }

    [CommandImplementation("makemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> MakeMutineerLeader([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => MakeMutineerLeader(ctx, marine));
    }

    [CommandImplementation("removemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public EntityUid RemoveMutineerLeader([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine)
    {
        RemComp<MutineerLeaderComponent>(marine);
        return marine;
    }

    [CommandImplementation("removemutineerleader"), AdminCommand(AdminFlags.Fun)]
    public IEnumerable<EntityUid> RemoveMutineerLeader([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines)
    {
        return marines.Select(marine => RemoveMutineerLeader(ctx, marine));
    }
}
