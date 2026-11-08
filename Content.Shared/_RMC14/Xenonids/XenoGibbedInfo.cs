using Robust.Shared.Serialization;

[Serializable, NetSerializable]
public sealed record GibbedXenoInfo
{
    public required string Name { get; init; }
    public required string LastPlayerId { get; init; }
}
