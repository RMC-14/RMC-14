namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public record struct XenoEvolveQueenAllowedEvent(EntityUid Xeno)
{
    public bool Allowed = true;
}
