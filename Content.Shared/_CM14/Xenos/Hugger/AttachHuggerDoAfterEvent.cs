using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Hugger;

[Serializable, NetSerializable]
public sealed partial class AttachHuggerDoAfterEvent : SimpleDoAfterEvent;
