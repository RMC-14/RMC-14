namespace Content.Shared._RMC14.Xenonids.Neurotoxin;

[ByRefEvent]
public record struct NeurotoxinInjectAttemptEvent(bool Cancelled = false);
