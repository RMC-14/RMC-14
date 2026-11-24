using Content.Client.UserInterface.Controls;
using System.Numerics;

namespace Content.Client.UserInterface.Systems.Inventory.Controls;

public sealed class ItemSlotButtonContainer : ItemSlotUIContainer<SlotControl>
{
    private readonly InventoryUIController _inventoryController;
    private string _slotGroup = "";

    public string SlotGroup
    {
        get => _slotGroup;
        set
        {
            _inventoryController.RemoveSlotGroup(SlotGroup);
            _slotGroup = value;
            _inventoryController.RegisterSlotGroupContainer(this);
        }
    }

    public ItemSlotButtonContainer()
    {
        _inventoryController = UserInterfaceManager.GetUIController<InventoryUIController>();
    }

    // RMC14
    internal Vector2 GetExpandedDesiredSize(Vector2 availableSize)
    {
        return ChildCount == 0 ? Vector2.Zero : base.MeasureOverride(availableSize);
    }
    // RMC14
}
