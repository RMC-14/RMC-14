using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[Flags]
[Serializable, NetSerializable]
public enum TacticalMapBlipFlags : byte
{
    None = 0,
    Dead = 1 << 0,
    Undefibbable = 1 << 1,
    HiveLeader = 1 << 2,
    Revealed = 1 << 3,
    RemotelyRemovable = 1 << 4,
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TacticalMapBlip(
    Vector2i Indices,
    SpriteSpecifier.Rsi Icon,
    SpriteSpecifier.Rsi? Background,
    Color Color,
    TacticalMapBlipFlags Flags,
    TimeSpan UnrevivableStart,
    TimeSpan UnrevivableEnd,
    float Opacity = 1f,
    string? Name = null,
    string? Role = null)
{
    public const int DefibStages = 5;

    public int GetDefibStage(TimeSpan now)
    {
        if (UnrevivableEnd <= UnrevivableStart)
            return 0;

        var progress = (now - UnrevivableStart) / (UnrevivableEnd - UnrevivableStart);
        return Math.Clamp((int) (DefibStages * progress), 0, DefibStages - 1);
    }
}
