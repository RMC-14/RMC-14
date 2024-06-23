using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Wounds;

[Serializable, NetSerializable]
public sealed partial class TreatWoundDoAfterEvent : SimpleDoAfterEvent;
