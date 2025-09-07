using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Animations;

[Serializable, NetSerializable]
public sealed class RMCFlickEvent(
    NetEntity entity,
    SpriteSpecifier.Rsi animationState,
    SpriteSpecifier.Rsi defaultState,
    string? layer) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
    public readonly SpriteSpecifier.Rsi AnimationState = animationState;
    public readonly SpriteSpecifier.Rsi DefaultState = defaultState;
    public readonly string? Layer = layer;
}
