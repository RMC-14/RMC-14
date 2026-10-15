namespace Content.Server._RMC14.Requisitions;

[ByRefEvent]
public readonly record struct RMCBlackMarketInvestigationEvent(EntityUid Actor, int Heat);
