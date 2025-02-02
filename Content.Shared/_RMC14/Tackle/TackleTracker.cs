using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tackle;

[Serializable, NetSerializable]
public record struct TackleTracker(int Count, TimeSpan Last);
