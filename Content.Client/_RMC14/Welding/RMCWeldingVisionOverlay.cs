using Content.Shared._RMC14.Welding;
using Content.Shared.Inventory;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Welding;

public sealed class RMCWeldingVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly InventorySystem _inventory;

    public RMCWeldingVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("RMCWeldingVision").InstanceUnique();
        _inventory = _entityManager.System<InventorySystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var local = _playerManager.LocalEntity;
        if (local == null)
            return false;

        if (!_entityManager.TryGetComponent(local.Value, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (_inventory.TryGetSlotEntity(local.Value, "eyes", out var eyes) &&
            _entityManager.HasComponent<RMCWeldingVisionComponent>(eyes.Value))
        {
            return true;
        }

        if (_inventory.TryGetSlotEntity(local.Value, "mask", out var mask) &&
            _entityManager.HasComponent<RMCWeldingVisionComponent>(mask.Value))
        {
            return true;
        }

        if (_inventory.TryGetSlotEntity(local.Value, "head", out var head) &&
            _entityManager.HasComponent<RMCWeldingVisionComponent>(head.Value))
        {
            return true;
        }

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
