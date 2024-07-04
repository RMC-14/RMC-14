using System.Numerics;
using Content.Shared._RMC14.Xenonids.Screech;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Screech;

public sealed class RMCXenoScreechShockWaveOverlay : Overlay, IEntityEventSubscriber
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SharedTransformSystem? _xformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    public RMCXenoScreechShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("RMCXenoScreechShockWave").Instance().Duplicate();
    }

    private Vector2 _position;
    private float _waveStrength;
    private float _waveSpeed;
    private float _downScale;
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || _xformSystem is null && !_entMan.TrySystem(out _xformSystem))
            return false;

        var query = _entMan.EntityQueryEnumerator<RMCXenoScreechShockWaveComponent, TransformComponent>();

        if (query.MoveNext(out var uid, out var distortion, out var xform))
        {
            if (xform.MapID != args.MapId)
                return false;

            var mapPos = _xformSystem.GetWorldPosition(uid);
            var tempCoords = args.Viewport.WorldToLocal(mapPos);

            // normalized coords, 0 - 1 plane. This is pure hell, we subtract 1 because fragment calculates from the bottom and local goes from the top of the viewport
            tempCoords.Y = 1 - (tempCoords.Y / args.Viewport.Size.Y);
            tempCoords.X /= args.Viewport.Size.X;

            _position = tempCoords;
            _waveStrength = distortion.WaveStrength;
            _waveSpeed = distortion.WaveSpeed;
            _downScale = distortion.DownScale;

            return true;
        }

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader?.SetParameter("position", _position);
        _shader?.SetParameter("waveSpeed", _waveSpeed);
        _shader?.SetParameter("downScale", _downScale);
        _shader?.SetParameter("waveStrength", _waveStrength);
        _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
