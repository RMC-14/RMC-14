using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PowerLoader;

[Serializable, NetSerializable]
public sealed partial class PowerLoaderGrabDoAfterEvent : SimpleDoAfterEvent;
