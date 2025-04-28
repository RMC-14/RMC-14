namespace Content.Shared._RMC14.MotionDetector;

public interface IDetectorComponent
{
    public List<Blip> Blips { get; set; }

    public TimeSpan LastScan { get; set; }

    public TimeSpan ScanDuration { get; set; }
}

