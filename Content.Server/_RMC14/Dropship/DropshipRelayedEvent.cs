namespace Content.Server._RMC14.Dropship;

[ByRefEvent]
public record struct DropshipRelayedEvent<TEvent>(TEvent Args, EntityUid Relayer);
