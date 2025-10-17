using Content.Shared._RMC14.Xenonids.Destroy;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Destroy;
public sealed partial class XenoDestroySystem : SharedXenoDestroySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float JumpHeight = 10;

    private const string LeapingAnimationKey = "king-leap-animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<XenoDestroyLeapStartEvent>(OnXenoLeapStart);
    }
    public Animation LeapAnimation(XenoDestroyComponent destroy, Vector2 leapOffset)
    {
        var midpoint = (leapOffset / 2);
        var opposite = -midpoint;

        midpoint += new Vector2(0, JumpHeight);
        opposite += new Vector2(0, JumpHeight);

        //How it works is simple
        //xeno goes from midpoint to midpoint, where midpoint is half the distance to the desired location
        //with extra y added so it feels like it's in the air
        //since the xeno gets moved halfway through theres an opposite midpoint so their general location is preserved

        var midtime = (float)(destroy.CrashTime.TotalSeconds / 2f);

        return new Animation
        {
            Length = destroy.CrashTime,
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

    private void OnXenoLeapStart(XenoDestroyLeapStartEvent ev)
    {
        if (!TryGetEntity(ev.King, out var xeno) || !TryComp<XenoDestroyComponent>(xeno, out var destroy))
            return;

        if (!TryComp<SpriteComponent>(xeno, out var sprite) || TerminatingOrDeleted(xeno))
            return;

        if (!TryComp<AnimationPlayerComponent>(xeno, out var player))
            return;

        if (_animPlayer.HasRunningAnimation(player, LeapingAnimationKey))
            return;


        _animPlayer.Play(xeno.Value, LeapAnimation(destroy, ev.LeapOffset), LeapingAnimationKey);
    }

    protected override void OnLeapingRemove(Entity<XenoDestroyLeapingComponent> xeno, ref ComponentRemove args)
    {
        base.OnLeapingRemove(xeno, ref args);

        if (!TryComp<SpriteComponent>(xeno, out var sprite) || TerminatingOrDeleted(xeno))
            return;

        if (TryComp(xeno, out AnimationPlayerComponent? animation))
            _animPlayer.Stop((xeno, animation), LeapingAnimationKey);

        _sprite.SetOffset((xeno.Owner, sprite), Vector2.Zero);
    }
}
