using Robust.Shared.Map;

namespace Content.Shared._RMC14.MotionDetector;

public interface IDetectorComponent
{
    public List<MapCoordinates> Blips { get; set; }

    public TimeSpan LastScan { get; set; }

    public TimeSpan ScanDuration { get; set; }
}

