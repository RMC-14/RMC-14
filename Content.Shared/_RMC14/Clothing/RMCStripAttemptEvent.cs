using Content.Shared._RMC14.Xenonids.ForTheHive;

namespace Content.Shared._RMC14.Clothing;

[ByRefEvent]
public record struct RMCStripItemAttemptEvent(bool Cancelled = false);
