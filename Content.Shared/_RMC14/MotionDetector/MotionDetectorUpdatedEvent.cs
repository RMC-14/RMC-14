namespace Content.Shared._RMC14.MotionDetector;

[ByRefEvent]
public readonly record struct MotionDetectorUpdatedEvent(bool Enabled);
