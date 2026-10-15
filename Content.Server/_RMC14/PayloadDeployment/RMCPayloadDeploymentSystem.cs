using System.Numerics;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.PayloadDeployment;

public sealed class RMCPayloadDeploymentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float StagingGroupSeparation = RMCPayloadDeploymentLimits.MaxLandingRadius * 2 + 100;

    private MapId? _stagingMap;
    private int _nextStagingGroup;
    private readonly HashSet<EntityUid> _pendingAnchors = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrashLandingComponent, CrashLandedEvent>(OnCrashLanded);
        SubscribeLocalEvent<ParaDroppingComponent, ParaDropFinishedEvent>(OnParaDropFinished);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnCrashLanded(Entity<CrashLandingComponent> ent, ref CrashLandedEvent args)
    {
        CompletePrototypePayload(ent.Owner);
    }

    private void OnParaDropFinished(Entity<ParaDroppingComponent> ent, ref ParaDropFinishedEvent args)
    {
        CompletePrototypePayload(ent.Owner);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _stagingMap = null;
        _nextStagingGroup = 0;
        _pendingAnchors.Clear();
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

    public void PreparePrototypePayload(EntityUid entity, EntityPrototype prototype)
    {
        if (!prototype.TryGetComponent(out TransformComponent? transform, _compFactory) ||
            !transform.Anchored)
        {
            return;
        }

        _transform.Unanchor(entity);
        _pendingAnchors.Add(entity);
    }

    public void CompletePrototypePayload(EntityUid entity)
    {
        if (_pendingAnchors.Remove(entity))
            _transform.AnchorEntity(entity);
    }

    public void CancelPrototypePayload(EntityUid entity)
    {
        _pendingAnchors.Remove(entity);
    }
}
