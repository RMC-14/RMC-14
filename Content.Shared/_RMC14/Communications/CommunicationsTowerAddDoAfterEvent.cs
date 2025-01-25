using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Communications;

[Serializable, NetSerializable]
public sealed partial class CommunicationsTowerAddDoAfterEvent : SimpleDoAfterEvent;
