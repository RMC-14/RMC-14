using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PayloadDeployment;

[Serializable, NetSerializable]
public readonly record struct RMCDropPrototypePayload(EntProtoId Prototype, int Quantity);

public static class RMCPayloadDeploymentLimits
{
    public const int MaxLandingRadius = 100;
    public const int MaxBatchRequests = 10;
    public const int MaxPayload = 500;
    public const int MaxOrbitalDrops = 100;
    public const float MaxTimingSeconds = 300;
}

public enum RMCPayloadDeploymentFailure : byte
{
    None,
    InvalidPayload,
    InvalidPrototype,
    InvalidSettings,
    InvalidTarget,
    InsufficientLandingTiles,
    PodPreparationFailed,
}

public readonly record struct RMCPayloadDeploymentResult(
    RMCPayloadDeploymentFailure Failure,
    int FailedRequest = -1,
    int RequestedLandings = 0,
    int AssignedLandings = 0)
{
    public bool Success => Failure == RMCPayloadDeploymentFailure.None;
}
