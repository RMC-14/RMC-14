using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[DataRecord]
[Serializable, NetSerializable]
public sealed record TechAnnounceEvent(string Author, string Message, SoundSpecifier? Sound);
