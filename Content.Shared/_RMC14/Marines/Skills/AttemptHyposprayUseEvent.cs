namespace Content.Shared._RMC14.Marines.Skills;

[ByRefEvent]
public record struct AttemptHyposprayUseEvent(EntityUid User, EntityUid Target, TimeSpan DoAfter)
{
    public void MaxDoAfter(TimeSpan b)
    {
        if (DoAfter < b)
            DoAfter = b;
    }
}
