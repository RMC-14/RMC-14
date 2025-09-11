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
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [CommandImplementation("is")]
    public EntityUid? Is(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid ent,
        [CommandArgument] MobState targetState)
    {
        if (!_entityManager.TryGetComponent<MobStateComponent>(ent, out var mobStateComponent))
            return null;

        return mobStateComponent.CurrentState == targetState ? ent : null;
    }

    [CommandImplementation("is")]
    public IEnumerable<EntityUid> Is(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> ents,
        [CommandArgument] MobState targetState)
    {
        return ents.Select(ent => Is(ctx, ent, targetState)).OfType<EntityUid>();
    }
}
