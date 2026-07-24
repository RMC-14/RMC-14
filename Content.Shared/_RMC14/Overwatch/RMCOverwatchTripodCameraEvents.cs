using Content.Shared._RMC14.Dialog;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[Serializable, NetSerializable]
public sealed partial class RMCOverwatchTripodDeployDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCOverwatchTripodPickupDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public record struct RMCOverwatchTripodDeployAttemptEvent(EntityUid User, bool Cancelled = false);

[Serializable, NetSerializable]
public sealed record RMCOverwatchTripodRenameInputEvent(NetEntity Camera, NetEntity User, string Message = "")
    : DialogInputEvent(Message);
