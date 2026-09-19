using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared.Eui;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin.PayloadDeployment;

public enum RMCPayloadDeliveryType : byte
{
    Orbital,
    ParaDrop,
}

[Serializable, NetSerializable]
public readonly record struct RMCPayloadDeploymentEntityEntry(
    NetEntity Entity,
    string Name,
    string Prototype,
    MapId Map,
    string Role,
    float Distance);

[Serializable, NetSerializable]
public readonly record struct RMCPayloadDeploymentMapEntry(
    MapId MapId,
    string Name,
    Vector2i CoordinateOffset,
    bool HasPlanetCoordinates);

[Serializable, NetSerializable]
public sealed class RMCPayloadDeploymentEuiState(
    List<RMCPayloadDeploymentEntityEntry> nearby,
    List<RMCPayloadDeploymentEntityEntry> playerControlled,
    List<RMCPayloadDeploymentMapEntry> maps,
    int nearbyRadius) : EuiStateBase
{
    public readonly List<RMCPayloadDeploymentEntityEntry> Nearby = nearby;
    public readonly List<RMCPayloadDeploymentEntityEntry> PlayerControlled = playerControlled;
    public readonly List<RMCPayloadDeploymentMapEntry> Maps = maps;
    public readonly int NearbyRadius = nearbyRadius;
}

[Serializable, NetSerializable]
public sealed class RMCPayloadDeploymentRefreshMsg(int nearbyRadius) : EuiMessageBase
{
    public readonly int NearbyRadius = nearbyRadius;
}

[Serializable, NetSerializable]
public sealed class RMCPayloadDeploymentValidateEntitiesMsg(List<NetEntity> entities) : EuiMessageBase
{
    public readonly List<NetEntity> Entities = entities;
}

[Serializable, NetSerializable]
public sealed class RMCPayloadDeploymentInvalidEntitiesMsg(List<NetEntity> entities) : EuiMessageBase
{
    public readonly List<NetEntity> Entities = entities;
}

[Serializable, NetSerializable]
public readonly record struct RMCOrbitalDropManifestMsg(
    List<NetEntity> Entities,
    List<RMCDropPrototypePayload> Prototypes,
    MapId TargetMap,
    Vector2i TargetCoordinates,
    float LandingRadius,
    bool UseDropPods,
    int PodCount,
    float ArrivalDelay,
    float DropDuration,
    float TimeToOpen,
    float LaunchInterval,
    float DropInterval,
    float DropIntervalVariation,
    bool UseParachute,
    bool ShowLandingWarning,
    bool RawCoordinates,
    bool IgnoreParadropRestrictions);

[Serializable, NetSerializable]
public sealed class RMCOrbitalDropBatchLaunchMsg(List<RMCOrbitalDropManifestMsg> manifests) : EuiMessageBase
{
    public readonly List<RMCOrbitalDropManifestMsg> Manifests = manifests;
}

[Serializable, NetSerializable]
public readonly record struct RMCParaDropManifestMsg(
    List<NetEntity> Entities,
    List<RMCDropPrototypePayload> Prototypes,
    MapId TargetMap,
    Vector2i TargetCoordinates,
    float LandingRadius,
    float ArrivalDelay,
    float DropDuration,
    float LaunchInterval,
    float ArrivalInterval,
    float ArrivalIntervalVariation,
    bool RawCoordinates,
    bool IgnoreParadropRestrictions);

[Serializable, NetSerializable]
public sealed class RMCParaDropBatchLaunchMsg(List<RMCParaDropManifestMsg> manifests) : EuiMessageBase
{
    public readonly List<RMCParaDropManifestMsg> Manifests = manifests;
}

[Serializable, NetSerializable]
public sealed class RMCPayloadDeploymentResultMsg(RMCPayloadDeploymentFailure failure, int failedManifest, int requestedLandings, int assignedLandings) : EuiMessageBase
{
    public readonly RMCPayloadDeploymentFailure Failure = failure;
    public readonly int FailedManifest = failedManifest;
    public readonly int RequestedLandings = requestedLandings;
    public readonly int AssignedLandings = assignedLandings;
}
