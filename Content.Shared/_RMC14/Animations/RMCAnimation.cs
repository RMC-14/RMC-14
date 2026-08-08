using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Animations;

[DataRecord]
[Serializable, NetSerializable]
public readonly partial record struct RMCAnimation(TimeSpan Length, List<RMCAnimationTrack> AnimationTracks);
