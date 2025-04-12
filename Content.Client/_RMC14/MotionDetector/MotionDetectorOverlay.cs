using System.Numerics;
using Content.Shared._RMC14.MotionDetector;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.MotionDetector;

public sealed class MotionDetectorOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private TimeSpan _last;
    private readonly List<Vector2> _blips = new();

    private MotionDetectorOverlaySystem _motionDetector;
    private SpriteSystem _sprite;

    public MotionDetectorOverlay()
    {
        IoCManager.InjectDependencies(this);
        _motionDetector = _entity.System<MotionDetectorOverlaySystem>();
        _sprite = _entity.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var frame = _sprite.GetFrame(new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Objects/Tools/motion_detector.rsi"), "detector_blip"), _timing.CurTime);
        _motionDetector.DrawBlips<MotionDetectorComponent>(args.WorldHandle, ref _last, _blips, frame);
    }
}
