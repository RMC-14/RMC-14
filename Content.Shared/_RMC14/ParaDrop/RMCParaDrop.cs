using Content.Shared._RMC14.PayloadDeployment;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.ParaDrop;

public sealed class RMCParaDropRequest
{
    public List<EntityUid> Entities { get; init; } = [];
    public List<RMCDropPrototypePayload> Prototypes { get; init; } = [];
    public MapCoordinates Target { get; init; }
    public float LandingRadius { get; init; }
    public float ArrivalDelay { get; init; } = 1.5f;
    public float DropDuration { get; init; } = 4;
    public float LaunchInterval { get; init; }
    public float ArrivalInterval { get; init; }
    public float ArrivalIntervalVariation { get; init; }
    public bool IgnoreParadropRestrictions { get; init; }
}
