using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Animations;

[DataRecord]
[Serializable, NetSerializable]
public record struct RMCAnimationId(string Id) : ISelfSerialize
{
    public void Deserialize(string value)
    {
        Id = value;
    }

    public string Serialize()
    {
        return Id;
    }
}
