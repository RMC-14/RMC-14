using System.Linq;
using Content.Shared._RMC14.Admin.PayloadDeployment;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Admin.PayloadDeployment;

public sealed class RMCPayloadDeploymentManifest
{
    public readonly Dictionary<NetEntity, RMCPayloadDeploymentEntityEntry> Entities = new();
    public readonly Dictionary<EntProtoId, int> Prototypes = new();

    public string Name = string.Empty;
    public MapId? Map;
    public Vector2i Coordinates;
    public float LandingRadius = 3;
    public bool UseDropPods = true;
    public int PodCount = 1;
    public float ArrivalDelay = 5;
    public float DropDuration = 3;
    public float OpenDelay = 2;
    public float LaunchInterval = 0.2f;
    public float ArrivalInterval = 0.2f;
    public float ArrivalIntervalVariation;
    public bool UseParachute;
    public bool ShowLandingWarning = true;
    public bool RawCoordinates;
    public bool IgnoreParadropRestrictions;

    public int PayloadCount()
    {
        return Entities.Count + Prototypes.Values.Sum();
    }

    public RMCPayloadDeploymentManifest CopySettings()
    {
        return new RMCPayloadDeploymentManifest
        {
            Map = Map,
            Coordinates = Coordinates,
            LandingRadius = LandingRadius,
            UseDropPods = UseDropPods,
            PodCount = PodCount,
            ArrivalDelay = ArrivalDelay,
            DropDuration = DropDuration,
            OpenDelay = OpenDelay,
            LaunchInterval = LaunchInterval,
            ArrivalInterval = ArrivalInterval,
            ArrivalIntervalVariation = ArrivalIntervalVariation,
            UseParachute = UseParachute,
            ShowLandingWarning = ShowLandingWarning,
            RawCoordinates = RawCoordinates,
            IgnoreParadropRestrictions = IgnoreParadropRestrictions,
        };
    }

    public RMCPayloadDeploymentManifest Clone()
    {
        var manifest = CopySettings();
        manifest.Name = Name;
        foreach (var (entity, entry) in Entities)
        {
            manifest.Entities.Add(entity, entry);
        }

        foreach (var (prototype, quantity) in Prototypes)
        {
            manifest.Prototypes.Add(prototype, quantity);
        }

        return manifest;
    }
}
