using Content.Server.CrewManifest;
using Content.Shared._RMC14.CrewManifest;
using Robust.Server.Player;

namespace Content.Server._RMC14.CrewManifest;

public sealed class RMCCrewManifestSystem : SharedRMCCrewManifestSystem
{
    [Dependency] private readonly CrewManifestSystem _crewManifest = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void OpenCrewManifest(EntityUid user, NetEntity station)
    {
        var stationEntity = GetEntity(station);
        OpenCrewManifest(user, stationEntity);
    }

    public override void OpenCrewManifest(EntityUid user, EntityUid station)
    {
        if (!_player.TryGetSessionByEntity(user, out var session))
            return;

        _crewManifest.OpenEui(station, session, user);
    }
}
