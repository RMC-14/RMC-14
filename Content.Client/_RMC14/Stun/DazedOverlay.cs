using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Stun;

public sealed class DazedOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _vignetteShader;

    public bool IsEnabled { get; set; }

    private float _outerFadeStart = 0.0f;
    private float _outerFadeEnd = 0.8f;
    private float _alpha = 1.0f;

    public DazedOverlay()
    {
        IoCManager.InjectDependencies(this);
        _vignetteShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!IsEnabled)
            return;

        var handle = args.WorldHandle;
        var viewport = args.WorldAABB;
        var distance = args.ViewportBounds.Width;

        _vignetteShader.SetParameter("color", new Vector3(0f, 0f, 0f));
        _vignetteShader.SetParameter("darknessAlphaOuter", _alpha);
        _vignetteShader.SetParameter("darknessAlphaInner", 0f);

        _vignetteShader.SetParameter("innerCircleRadius", _outerFadeStart * distance * 0.5f);
        _vignetteShader.SetParameter("innerCircleMaxRadius", _outerFadeStart * distance * 0.5f);

        _vignetteShader.SetParameter("outerCircleRadius", _outerFadeEnd * distance * 0.5f);
        _vignetteShader.SetParameter("outerCircleMaxRadius", _outerFadeEnd * distance * 0.5f);

        handle.UseShader(_vignetteShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
