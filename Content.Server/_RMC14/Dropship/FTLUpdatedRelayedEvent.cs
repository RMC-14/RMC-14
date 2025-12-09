namespace Content.Server._RMC14.Dropship;

[ByRefEvent]
public record struct FTLUpdatedRelayedEvent<TEvent>(TEvent Args, EntityUid Relayer);
