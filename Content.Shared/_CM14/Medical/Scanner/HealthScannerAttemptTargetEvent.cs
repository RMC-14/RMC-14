namespace Content.Shared._CM14.Medical.Scanner;

[ByRefEvent]
public record struct HealthScannerAttemptTargetEvent(string? Popup = null, bool Cancelled = false);
