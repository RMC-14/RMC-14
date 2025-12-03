using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class GetUsernameCommand : ToolshedCommand
{
    [Dependency] private readonly IPlayerManager _players = default!;

    [CommandImplementation]
    public string? GetUsername([PipedArgument] EntityUid entity)
    {
        if (!_players.TryGetSessionByEntity(entity, out var session))
            return null;

        return session.Data.UserName;
    }

    [CommandImplementation]
    public IEnumerable<string?> GetUsername([PipedArgument] IEnumerable<EntityUid> entities)
    {
        return entities.Select(entity => GetUsername(entity));
    }
}
