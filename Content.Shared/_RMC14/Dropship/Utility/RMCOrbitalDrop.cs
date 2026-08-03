using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Utility;

[Serializable, NetSerializable]
public readonly record struct RMCOrbitalDropPrototypePayload(EntProtoId Prototype, int Quantity);

public sealed class RMCOrbitalDropRequest
{
    public const int MaxLandingRadius = 100;
    public const int MaxPayload = 500;
    public const int MaxPods = 100;
    public const float MaxTimingSeconds = 300;

    public List<EntityUid> Entities { get; init; } = [];
    public List<RMCOrbitalDropPrototypePayload> Prototypes { get; init; } = [];
    public MapCoordinates Target { get; init; }
    public int LandingRadius { get; init; }
    public int PodCount { get; init; } = 1;
    public float ArrivalDelay { get; init; } = 5;
    public float DropDuration { get; init; } = 3;
    public float TimeToOpen { get; init; } = 2;
    public float LaunchInterval { get; init; } = 0.2f;
    public float DropInterval { get; init; } = 0.2f;
    public float DropIntervalVariation { get; init; }
    public bool UseParachute { get; init; } = true;
    public bool IgnoreParadropRestrictions { get; init; }
    public EntityUid? LaunchSource { get; init; }
}

public enum RMCOrbitalDropFailure : byte
{
    None,
    InvalidPayload,
    InvalidPrototype,
    InvalidSettings,
    InvalidTarget,
    InsufficientLandingTiles,
    PodPreparationFailed,
}

public readonly record struct RMCOrbitalDropResult(
    RMCOrbitalDropFailure Failure,
    int RequestedLandingTiles = 0,
    int ViableLandingTiles = 0)
{
    public bool Success => Failure == RMCOrbitalDropFailure.None;
}
