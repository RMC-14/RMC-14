using Content.Shared._RMC14.Welding;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
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
    private bool _isSuperior;

    public RMCWeldingVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("RMCWeldingVision").InstanceUnique();
        _inventory = _entityManager.System<InventorySystem>();
    }

    private bool TryGetWeldingVision(EntityUid? item, out bool superior)
    {
        superior = false;

        if (item == null)
            return false;

        if (!_entityManager.TryGetComponent<RMCWeldingVisionComponent>(item.Value, out var weldComp))
            return false;

        if (_entityManager.TryGetComponent<ItemToggleComponent>(item.Value, out var toggle) &&
            !toggle.Activated)
        {
            return false;
        }

        superior = weldComp.Superior;
        return true;
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

        _isSuperior = false;

        _inventory.TryGetSlotEntity(local.Value, "eyes", out var eyes);
        _inventory.TryGetSlotEntity(local.Value, "mask", out var mask);
        _inventory.TryGetSlotEntity(local.Value, "head", out var head);

        if (TryGetWeldingVision(eyes, out var eyesSuperior))
        {
            _isSuperior = eyesSuperior;
            return true;
        }

        if (TryGetWeldingVision(mask, out var maskSuperior))
        {
            _isSuperior = maskSuperior;
            return true;
        }

        if (TryGetWeldingVision(head, out var headSuperior))
        {
            _isSuperior = headSuperior;
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

        if (_isSuperior)
        {
            _shader.SetParameter("min_brightness", 0.22f);
            _shader.SetParameter("max_brightness", 0.75f);
        }
        else
        {
            _shader.SetParameter("min_brightness", 0.0f);
            _shader.SetParameter("max_brightness", 0.6f);
        }

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
