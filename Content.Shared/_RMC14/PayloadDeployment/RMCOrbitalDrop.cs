using Robust.Shared.Map;

namespace Content.Shared._RMC14.PayloadDeployment;

public sealed class RMCOrbitalDropRequest
{
    public List<EntityUid> Entities { get; init; } = [];
    public List<RMCDropPrototypePayload> Prototypes { get; init; } = [];
    public MapCoordinates Target { get; init; }
    public float LandingRadius { get; init; }
    public bool UseDropPods { get; init; } = true;
    public int PodCount { get; init; } = 1;
    public float ArrivalDelay { get; init; } = 5;
    public float DropDuration { get; init; } = 3;
    public float TimeToOpen { get; init; } = 2;
    public float LaunchInterval { get; init; } = 0.2f;
    public float DropInterval { get; init; } = 0.2f;
    public float DropIntervalVariation { get; init; }
    public bool UseParachute { get; init; } = true;
    public bool ShowLandingWarning { get; init; } = true;
    public bool IgnoreParadropRestrictions { get; init; }
}
