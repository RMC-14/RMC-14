using System.Linq;
using Content.Server.Administration;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class MobStateCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid? Is(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid ent,
        [CommandArgument] MobState targetState,
        [CommandInverted] bool inverted)
    {
        var query = GetEntityQuery<MobStateComponent>();

        if (query.TryComp(ent, out var comp))
        {
            var matches = comp.CurrentState == targetState;
            return inverted ? (matches ? null : ent) : (matches ? ent : null);
        }

        return inverted ? ent : null;
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Is(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> ents,
        [CommandArgument] MobState targetState,
        [CommandInverted] bool inverted)
    {
        return ents.Select(ent => Is(ctx, ent, targetState, inverted)).OfType<EntityUid>();
    }
}
