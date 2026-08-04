using System.Numerics;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared.GameTicking;
using Robust.Shared.Map;

namespace Content.Server._RMC14.PayloadDeployment;

public sealed class RMCPayloadDeploymentSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    private const float StagingGroupSeparation = RMCPayloadDeploymentLimits.MaxLandingRadius * 2 + 100;

    private MapId? _stagingMap;
    private int _nextStagingGroup;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _stagingMap = null;
        _nextStagingGroup = 0;
    }

    public Vector2 AllocateStagingGroup()
    {
        return new Vector2(_nextStagingGroup++ * StagingGroupSeparation, 0);
    }

    public MapCoordinates GetStagingCoordinates(Vector2 position)
    {
        if (_stagingMap is not { } mapId || !_map.MapExists(mapId))
        {
            _map.CreateMap(out mapId);
            _stagingMap = mapId;
        }

        return new MapCoordinates(position, mapId);
    }
}
