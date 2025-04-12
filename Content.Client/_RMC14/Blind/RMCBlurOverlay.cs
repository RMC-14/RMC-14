using Content.Shared._RMC14.BlurredVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Blind;

public sealed class RMCBlurOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _blurShader;

    private const float BlurAmount = 0.01f;

    public RMCBlurOverlay(IEntityManager entManager)
    {
        IoCManager.InjectDependencies(this);
        _blurShader = _prototypeManager.Index<ShaderPrototype>("RMCBlurryVisionX").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (!_entityManager.HasComponent<RMCBlindedComponent>(_playerManager.LocalEntity))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        var handle = args.WorldHandle;
        _blurShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _blurShader.SetParameter("BLUR_AMOUNT", BlurAmount);
        handle.UseShader(_blurShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
