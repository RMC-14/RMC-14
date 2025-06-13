using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Refill;

[Serializable, NetSerializable]
public sealed partial class ContainerFlushDoAfterEvent : SimpleDoAfterEvent;
