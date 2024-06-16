using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.IV;

[Serializable, NetSerializable]
public sealed partial class AttachBloodPackDoAfterEvent : SimpleDoAfterEvent;
