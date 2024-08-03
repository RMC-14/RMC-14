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

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _eyeProtShader;

    private RMCEyeProtectionComponent _eyeProtComponent = default!;

    public RMCEyeProtectionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _eyeProtShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<RMCEyeProtectionComponent>(playerEntity, out var eyeProtComp))
            return false;

        _eyeProtComponent = eyeProtComp;

        return (_eyeProtComponent.State == EyeProtectionState.On);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out RMCEyeProtectionComponent? eyeProt) ||
            eyeProt.State == EyeProtectionState.Off)
        {
            return;
        }

        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var content))
        {
            _eyeProtShader?.SetParameter("Zoom", 0.20f * content.Zoom.X);
        }

        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        handle.UseShader(_eyeProtShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
