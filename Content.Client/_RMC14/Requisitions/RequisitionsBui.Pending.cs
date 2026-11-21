using System.Linq;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private readonly List<RequisitionsPendingOrderCard> _pendingCards = new();

    private void PopulatePendingOrders()
    {
        if (_window == null || !_window.PendingTab.Visible)
            return;

        if (_state == null || _state.PendingOrders.Count == 0)
        {
            foreach (var card in _pendingCards)
                card.Dispose();
            _pendingCards.Clear();
            SetText(_window.PendingStatusLabel, Loc.GetString("rmc-requisitions-pending-empty"));
            return;
        }

        var search = _window.SearchBar.Text.Trim();
        var visibleOrders = new List<(RequisitionsPendingOrder Order, int? Category, ProductDisplay Display)>();
        foreach (var order in _state.PendingOrders)
        {
            var category = GetPendingOrderCategory(order.Entry);
            if (!ShouldShowPendingOrder(order.Entry, category))
                continue;

            var display = GetDisplay(order.Entry);
            if (!MatchesSearch(display, search))
                continue;

            visibleOrders.Add((order, category, display));
        }

        visibleOrders.Sort((a, b) =>
        {
            var categoryComparison = (a.Category ?? int.MaxValue).CompareTo(b.Category ?? int.MaxValue);
            if (categoryComparison != 0)
                return categoryComparison;

            return string.Compare(a.Display.Name, b.Display.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        var position = 0;
        foreach (var (order, _, display) in visibleOrders)
        {
            if (position == _pendingCards.Count)
            {
                var added = new RequisitionsPendingOrderCard();
                _pendingCards.Add(added);
                _window.PendingContainer.AddChild(added);
            }
            var card = _pendingCards[position++];
            SetText(card.Cost, GetCostText(order.Entry));
            SetText(card.Quantity, Loc.GetString("rmc-requisitions-pending-quantity", ("amount", order.Amount)));
            if (!card.Icon.Textures.SequenceEqual(display.IconTextures))
                card.Icon.Textures = display.IconTextures;
            card.Icon.Modulate = display.IconModulate;
            SetText(card.ProductName, display.Name, color: Color.White);
            SetText(card.Description, display.Description);
            SetText(card.OrderDetails, order.Entry.Request is { } request ? Loc.GetString(
                    "rmc-requisitions-request-manifest",
                    ("number", request.Number), ("requester", request.RequestedBy), ("approver", request.ApprovedBy))
                : string.Empty);
            card.OrderDetails.Visible = order.Entry.Request != null;
        }
        while (_pendingCards.Count > position)
        {
            _pendingCards[^1].Dispose();
            _pendingCards.RemoveAt(_pendingCards.Count - 1);
        }

        var status = (visibleOrders.Count, string.IsNullOrWhiteSpace(search)) switch
        {
            (0, false) => Loc.GetString("rmc-requisitions-pending-filter-empty"),
            (0, true) => Loc.GetString("rmc-requisitions-pending-category-empty"),
            _ => string.Empty,
        };
        SetText(_window.PendingStatusLabel, status);
    }

    private int? GetPendingOrderCategory(RequisitionsEntry entry)
    {
        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return null;

        var categories = entry.BlackMarket ? computer.BlackMarketCategories : computer.Categories;
        for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            var category = categories[categoryIndex];
            if (entry.NoReturnReward && entry.Entities.Count > 0 &&
                category.Entries.Any(product => product.Unit && entry.Entities.Contains(product.Crate)))
                return categoryIndex;

            foreach (var categoryEntry in category.Entries)
            {
                if (SamePendingEntry(categoryEntry, entry))
                    return categoryIndex;
            }
        }

        return null;
    }

    private bool ShouldShowPendingOrder(RequisitionsEntry entry, int? category)
    {
        if (_blackMarketMode)
        {
            if (!entry.BlackMarket)
                return false;

            return true;
        }

        if (SelectedCategory == null)
            return true;

        if (!entry.BlackMarket && entry.NoReturnReward && _state is { } computer)
        {
            return computer.Categories[SelectedCategory.Value].Entries
                .Any(product => product.Unit && entry.Entities.Contains(product.Crate));
        }

        return !entry.BlackMarket && category == SelectedCategory;
    }

    private static bool SamePendingEntry(RequisitionsEntry a, RequisitionsEntry b)
    {
        if (a.Crate != b.Crate ||
            a.Cost != b.Cost ||
            a.BlackMarket != b.BlackMarket ||
            a.BlackMarketCost != b.BlackMarketCost ||
            a.BlackMarketHeat != b.BlackMarketHeat ||
            a.Name != b.Name ||
            a.Description != b.Description ||
            !Equals(a.Icon, b.Icon) ||
            a.Entities.Count != b.Entities.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Entities.Count; i++)
        {
            if (a.Entities[i] != b.Entities[i])
                return false;
        }

        return true;
    }
}
