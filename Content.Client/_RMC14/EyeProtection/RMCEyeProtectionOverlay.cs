using Content.Shared._RMC14.EyeProtection;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.EyeProtection;

public sealed class RMCEyeProtectionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _eyeProtShader;

    private readonly float _maxTilesHeight = 8.5f;

    private Vector3 _color = new Vector3(0f, 0f, 0f);
    private float _outerRadius;
    private float _innerRadius;
    private float _darknessAlphaOuter = 1.0f;
    private float _darknessAlphaInner = 0.0f;

    public RMCEyeProtectionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _eyeProtShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        if (!_entityManager.TryGetComponent<RMCSightRestrictionComponent>(playerEntity, out var eyeProt))
            return;

        var handle = args.WorldHandle;
        var viewport = args.WorldAABB;
        var viewportHeight = args.ViewportBounds.Height;

        // Default view distance from top to bottom of screen in tiles

        // Actual height of viewport in tiles, accounting for zoom
        var actualTilesHeight = _maxTilesHeight * eyeComp.Zoom.X;

        var outerRadiusRatio = (_maxTilesHeight - eyeProt.ImpairFull) / actualTilesHeight / 2;
        var innerRadiusRatio = (_maxTilesHeight - eyeProt.ImpairFull - eyeProt.ImpairPartial) / actualTilesHeight / 2;

        _innerRadius = innerRadiusRatio * viewportHeight;
        _outerRadius = outerRadiusRatio * viewportHeight;
        _darknessAlphaInner = eyeProt.AlphaInner;
        _darknessAlphaOuter = eyeProt.AlphaOuter;

        // Shouldn't be time-variant
        _eyeProtShader.SetParameter("time", 0.0f);
        // Outside area should be black
        _eyeProtShader.SetParameter("color", _color);
        _eyeProtShader.SetParameter("darknessAlphaInner", _darknessAlphaInner);
        _eyeProtShader.SetParameter("darknessAlphaOuter", _darknessAlphaOuter);
        // Radius should stay constant
        _eyeProtShader.SetParameter("outerCircleRadius", _outerRadius);
        _eyeProtShader.SetParameter("outerCircleMaxRadius", _outerRadius);
        _eyeProtShader.SetParameter("innerCircleRadius", _innerRadius);
        _eyeProtShader.SetParameter("innerCircleMaxRadius", _innerRadius);

        handle.UseShader(_eyeProtShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
