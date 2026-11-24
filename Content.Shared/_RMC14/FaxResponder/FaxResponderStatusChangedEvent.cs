namespace Content.Shared._RMC14.FaxResponder;

[ByRefEvent]
public readonly record struct FaxResponderStatusChangedEvent(List<FaxResponderJobStatus> Jobs);
