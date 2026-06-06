using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.Xeno;

[Serializable, NetSerializable]
public sealed record ResinMarkerTrackerSelectTargetEvent(NetEntity Target);
