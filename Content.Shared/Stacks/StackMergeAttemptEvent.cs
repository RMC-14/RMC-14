namespace Content.Shared.Stacks;

[ByRefEvent]
public record struct StackMergeAttemptEvent(EntityUid Other)
{
    public bool Cancelled;
}
