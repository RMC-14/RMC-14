using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.PillBottle;

[Serializable, NetSerializable]
public sealed partial class RMCPillBottleBreakDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCPillBottleRepairDoAfterEvent : SimpleDoAfterEvent;
