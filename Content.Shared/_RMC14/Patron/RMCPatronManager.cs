using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Patron;

public sealed class RMCPatronManager
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    // public bool IsLocalPatron()
    // {
    //     return _player.LocalUser is { } user && IsPatron(user);
    // }
    //
    // public bool IsPatron(NetUserId player)
    // {
    //
    // }
}
