using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Repairable;

[Serializable, NetSerializable]
public sealed partial class RMCRepairableDoAfterEvent : SimpleDoAfterEvent;
