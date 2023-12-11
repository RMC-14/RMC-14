using System.IO;
using Content.Server.Administration.Managers;
using Content.Shared._CM14.Mapping;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._CM14.Mapping;

public sealed class MappingManager : IPostInjectInit
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public void PostInject()
    {
#if !FULL_RELEASE
        _net.RegisterNetMessage<MappingSaveMapMessage>(OnMappingSaveMap);
#endif
    }

    private void OnMappingSaveMap(MappingSaveMapMessage message)
    {
#if !FULL_RELEASE
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            !_admin.IsAdmin(session, true) ||
            !_admin.HasAdminFlag(session, AdminFlags.Host) ||
            session.AttachedEntity is not { } player)
        {
            return;
        }

        var mapId = _systems.GetEntitySystem<TransformSystem>().GetMapCoordinates(player).MapId;
        _systems.GetEntitySystem<MapLoaderSystem>().SaveMap(mapId, message.Path);
#endif
    }
}
