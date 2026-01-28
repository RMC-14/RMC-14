using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.IV;

[Serializable, NetSerializable]
public sealed partial class AttachDialysisDoAfterEvent : SimpleDoAfterEvent;
