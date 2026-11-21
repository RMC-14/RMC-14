namespace Content.Shared._RMC14.FaxResponder;

public sealed class FaxResponderStatusChangedEvent(List<FaxResponderJobStatus> jobs) : EntityEventArgs
{
    public List<FaxResponderJobStatus> Jobs { get; } = jobs;
}
