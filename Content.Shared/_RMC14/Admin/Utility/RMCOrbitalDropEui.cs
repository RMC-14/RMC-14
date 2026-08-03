using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared.Eui;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin.Utility;

[Serializable, NetSerializable]
public readonly record struct RMCOrbitalDropEntityEntry(
    NetEntity Entity,
    string Name,
    string Prototype,
    string Map,
    float Distance);

[Serializable, NetSerializable]
public readonly record struct RMCOrbitalDropMapEntry(
    MapId MapId,
    string Name,
    Vector2i CoordinateOffset,
    bool HasPlanetCoordinates);

[Serializable, NetSerializable]
public sealed class RMCOrbitalDropEuiState(
    List<RMCOrbitalDropEntityEntry> nearby,
    List<RMCOrbitalDropEntityEntry> playerControlled,
    List<RMCOrbitalDropMapEntry> maps,
    float nearbyRadius) : EuiStateBase
{
    public readonly List<RMCOrbitalDropEntityEntry> Nearby = nearby;
    public readonly List<RMCOrbitalDropEntityEntry> PlayerControlled = playerControlled;
    public readonly List<RMCOrbitalDropMapEntry> Maps = maps;
    public readonly float NearbyRadius = nearbyRadius;
}

[Serializable, NetSerializable]
public sealed class RMCOrbitalDropRefreshMsg(float nearbyRadius) : EuiMessageBase
{
    public readonly float NearbyRadius = nearbyRadius;
}

[Serializable, NetSerializable]
public sealed class RMCOrbitalDropLaunchMsg(
    List<NetEntity> entities,
    List<RMCOrbitalDropPrototypePayload> prototypes,
    MapId targetMap,
    Vector2i targetCoordinates,
    int landingRadius,
    int podCount,
    float arrivalDelay,
    float dropDuration,
    float timeToOpen,
    float launchInterval,
    float dropInterval,
    float dropIntervalVariation,
    bool useParachute,
    bool rawCoordinates,
    bool ignoreParadropRestrictions) : EuiMessageBase
{
    public readonly List<NetEntity> Entities = entities;
    public readonly List<RMCOrbitalDropPrototypePayload> Prototypes = prototypes;
    public readonly MapId TargetMap = targetMap;
    public readonly Vector2i TargetCoordinates = targetCoordinates;
    public readonly int LandingRadius = landingRadius;
    public readonly int PodCount = podCount;
    public readonly float ArrivalDelay = arrivalDelay;
    public readonly float DropDuration = dropDuration;
    public readonly float TimeToOpen = timeToOpen;
    public readonly float LaunchInterval = launchInterval;
    public readonly float DropInterval = dropInterval;
    public readonly float DropIntervalVariation = dropIntervalVariation;
    public readonly bool UseParachute = useParachute;
    public readonly bool RawCoordinates = rawCoordinates;
    public readonly bool IgnoreParadropRestrictions = ignoreParadropRestrictions;
}

[Serializable, NetSerializable]
public sealed class RMCOrbitalDropResultMsg(
    RMCOrbitalDropFailure failure,
    int requestedLandingTiles,
    int viableLandingTiles) : EuiMessageBase
{
    public readonly RMCOrbitalDropFailure Failure = failure;
    public readonly int RequestedLandingTiles = requestedLandingTiles;
    public readonly int ViableLandingTiles = viableLandingTiles;
}
