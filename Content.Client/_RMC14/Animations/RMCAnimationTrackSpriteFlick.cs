using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Animations;

public sealed class RMCAnimationTrackSpriteFlick : AnimationTrack
{
    /// <summary>
    ///     A list of key frames for when to fire flicks.
    /// </summary>
    public required List<KeyFrame> KeyFrames { get; init; }

    /// <summary>
    ///     The layer key of the layer to flick on.
    /// </summary>
    public required string LayerKey { get; init; }

    public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
    {
        return (-1, 0);
    }

    public override (int KeyFrameIndex, float FramePlayingTime)
        AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
    {
        DebugTools.AssertNotNull(LayerKey);

        var entity = (EntityUid) context;
        var entities = IoCManager.Resolve<IEntityManager>();
        var sprite = entities.GetComponent<SpriteComponent>(entity);
        var ent = new Entity<SpriteComponent>(entity, sprite);
        var spriteSystem = entities.System<SpriteSystem>();

        var playingTime = prevPlayingTime + frameTime;
        var keyFrameIndex = prevKeyFrameIndex;
        // Advance to the correct key frame.
        while (keyFrameIndex != KeyFrames.Count - 1 && KeyFrames[keyFrameIndex + 1].KeyTime < playingTime)
        {
            playingTime -= KeyFrames[keyFrameIndex + 1].KeyTime;
            keyFrameIndex += 1;
        }

        if (keyFrameIndex >= 0)
        {
            var keyFrame = KeyFrames[keyFrameIndex];
            // Advance animation on current key frame.
            if (!spriteSystem.TryGetLayer(ent.AsNullable(), LayerKey, out var layer, false))
                return (keyFrameIndex, playingTime);

            var rsi = layer.ActualRsi;
            if (rsi != null && rsi.TryGetState(keyFrame.Rsi.RsiState, out var state))
            {
                var animationTime = Math.Min(state.AnimationLength - 0.01f, playingTime);
                spriteSystem.LayerSetAutoAnimated(layer, false);
                // TODO: Doesn't setting the state explicitly reset the animation
                // so it's slightly more inefficient?
                spriteSystem.LayerSetSprite(layer, keyFrame.Rsi);
                spriteSystem.LayerSetRsiState(layer, keyFrame.Rsi.RsiState);
                spriteSystem.LayerSetAnimationTime(layer, animationTime);
            }
        }

        return (keyFrameIndex, playingTime);
    }

    public readonly record struct KeyFrame(SpriteSpecifier.Rsi Rsi, float KeyTime);
}
