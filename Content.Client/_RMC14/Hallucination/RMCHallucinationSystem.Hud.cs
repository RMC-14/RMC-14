using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hands;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Shared._RMC14.Hallucination;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private readonly List<FakeItem> _items = new();

    private void InitializeHud()
    {
        SubscribeNetworkEvent<RMCHallucinationItemEvent>(OnItem);
    }

    private void OnItem(RMCHallucinationItemEvent ev)
    {
        SlotControl? button;
        if (ev.SlotGroup == null)
        {
            _ui.GetUIController<HandsUIController>().TryGetHandButton(ev.Slot, out var hand);
            button = hand;
        }
        else
        {
            _ui.GetUIController<InventoryUIController>().TryGetSlotButton(ev.SlotGroup, ev.Slot, out button);
        }

        if (button == null || button.Entity != null)
            return;

        var fake = Spawn(ev.Prototype);
        button.SetEntity(fake);

        TrackOwner();
        _items.Add(new FakeItem(fake, button, _timing.CurTime + ev.Duration));
    }

    private void UpdateHud(TimeSpan time)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            if (time < item.End)
                continue;

            _items.RemoveAt(i);
            RemoveItem(item);
        }
    }

    private void ClearHud()
    {
        foreach (var item in _items)
        {
            RemoveItem(item);
        }

        _items.Clear();
    }

    private void RemoveItem(FakeItem item)
    {
        if (item.Button.Entity == item.Uid)
            item.Button.SetEntity(null);

        QueueDel(item.Uid);
    }

    private sealed record FakeItem(EntityUid Uid, SlotControl Button, TimeSpan End);
}
