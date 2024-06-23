using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Attachable.Events;

[Serializable, NetSerializable]
public sealed partial class AttachableDetachDoAfterEvent : SimpleDoAfterEvent;
