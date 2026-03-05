using Content.Client.Overlays;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Ping;

public abstract class RMCPingOverlaySystem<TComponent, TOverlay> : EquipmentHudSystem<TComponent>
    where TComponent : Component
    where TOverlay : Overlay
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private TOverlay _overlay = default!;

    protected override SlotFlags TargetSlots => SlotFlags.NONE;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = CreateOverlay();
    }

    protected abstract TOverlay CreateOverlay();

    protected override void UpdateInternal(RefreshEquipmentHudEvent<TComponent> component)
    {
        base.UpdateInternal(component);

        if (!_overlayManager.HasOverlay<TOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayManager.RemoveOverlay(_overlay);
    }
}
