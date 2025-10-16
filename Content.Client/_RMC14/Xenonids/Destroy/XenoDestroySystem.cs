using Content.Shared._RMC14.Xenonids.Destroy;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Destroy;
public sealed class XenoDestroySystem : SharedXenoDestroySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float JumpHeight = 10;

    private const string LeapingAnimationKey = "king-leap-animation";
    public Animation LeapAnimation(XenoDestroyComponent leaping)
    {
        if (leaping.LastTargetCoords == null)
            return new Animation();

        var midpoint = (leaping.LastTargetCoords.Value.Position / 2);
        var opposite = -midpoint;

        midpoint += new Vector2(0, JumpHeight);
        opposite += new Vector2(0, JumpHeight);

        //How it works is simple
        //xeno goes from midpoint to midpoint, where midpoint is half the distance to the desired location
        //with extra y added so it feels like it's in the air
        //since the xeno gets moved halfway through theres an opposite midpoint so their general location is preserved

        var midtime = (float)(leaping.CrashTime.TotalSeconds / 2f);

        return new Animation
        {
            Length = leaping.CrashTime,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(midpoint, midtime),
                        new AnimationTrackProperty.KeyFrame(opposite, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, midtime),
                    }
                }
            }
        };
    }

    protected override void OnLeapingInit(Entity<XenoDestroyLeapingComponent> xeno, ref ComponentStartup args)
    {
        base.OnLeapingInit(xeno, ref args);

        if (!TryComp<SpriteComponent>(xeno, out var sprite) || TerminatingOrDeleted(xeno) || _timing.ApplyingState)
            return;

        if (!TryComp<XenoDestroyComponent>(xeno, out var dest))
            return;

        if (!TryComp<AnimationPlayerComponent>(xeno, out var player))
            return;

        if (_animPlayer.HasRunningAnimation(player, LeapingAnimationKey))
            return;


        _animPlayer.Play((xeno, player), LeapAnimation(dest), LeapingAnimationKey);
    }

    protected override void OnLeapingRemove(Entity<XenoDestroyLeapingComponent> xeno, ref ComponentShutdown args)
    {
        base.OnLeapingRemove(xeno, ref args);

        if (!TryComp<SpriteComponent>(xeno, out var sprite) || TerminatingOrDeleted(xeno))
            return;

        //Hope this fixes itself tomorrow for no reason
        if (TryComp(xeno, out AnimationPlayerComponent? animation))
            _animPlayer.Stop((xeno, animation), LeapingAnimationKey);

        _sprite.SetOffset((xeno.Owner, sprite), Vector2.Zero);
    }
}
