namespace Content.Shared._RMC14.MotionDetector;

[ByRefEvent]
public readonly record struct MotionDetectorScanEvent(EntityUid Detector, float Range, List<Blip> Blips);
