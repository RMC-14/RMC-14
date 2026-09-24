using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Acid;

[Serializable, NetSerializable]
public sealed partial class XenoAcidHoleCrawlDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoAcidHoleRepairDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoAcidHoleBreakDoAfterEvent : SimpleDoAfterEvent;
