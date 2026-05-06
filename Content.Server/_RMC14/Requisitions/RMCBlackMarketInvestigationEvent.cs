namespace Content.Server._RMC14.Requisitions;

public sealed class RMCBlackMarketInvestigationEvent(EntityUid actor, int heat) : EntityEventArgs
{
    public readonly EntityUid Actor = actor;
    public readonly int Heat = heat;
}
