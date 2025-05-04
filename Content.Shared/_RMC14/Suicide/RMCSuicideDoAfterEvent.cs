using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Suicide;

[Serializable, NetSerializable]
public sealed partial class RMCSuicideDoAfterEvent : SimpleDoAfterEvent;
