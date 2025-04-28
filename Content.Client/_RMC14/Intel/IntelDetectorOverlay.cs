using System.Numerics;
using Content.Client._RMC14.MotionDetector;
using Content.Shared._RMC14.Intel.Detector;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Intel;

public sealed class IntelDetectorOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private TimeSpan _last;
    private readonly List<Vector2> _blips = new();

    private MotionDetectorOverlaySystem _motionDetector;
    private SpriteSystem _sprite;

    public IntelDetectorOverlay()
    {
        IoCManager.InjectDependencies(this);
        _motionDetector = _entity.System<MotionDetectorOverlaySystem>();
        _sprite = _entity.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var frame = _sprite.GetFrame(new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Objects/Tools/intel_detector.rsi"), "data_blip"), _timing.CurTime);
        _motionDetector.DrawBlips<IntelDetectorComponent>(args.WorldHandle, ref _last, _blips, frame);
    }
}
