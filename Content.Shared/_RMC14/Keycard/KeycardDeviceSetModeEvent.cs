using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Keycard;

[Serializable, NetSerializable]
public sealed record KeycardDeviceSetModeEvent(KeycardDeviceMode Mode);
