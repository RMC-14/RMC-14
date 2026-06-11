using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.UniversalRecorder;

[Serializable, NetSerializable]
public sealed partial class UniversalRecorderTapeRespoolDoAfterEvent : SimpleDoAfterEvent;
