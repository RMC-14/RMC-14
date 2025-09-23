using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Animations;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCAnimationTrack(object? LayerKey, List<RMCKeyFrame> KeyFrames);
