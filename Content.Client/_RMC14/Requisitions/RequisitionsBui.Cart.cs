using System.Linq;
using Content.Shared._RMC14.Requisitions;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private readonly Dictionary<CartKey, int> _cart = new();
    private readonly Dictionary<CartKey, int> _blackMarketCart = new();
    private readonly Dictionary<CartKey, RequisitionsCartRow> _cartRows = new();
    private readonly HashSet<CartKey> _shownCartRows = new();
    private bool _cartRowsBlackMarket;
    private CatalogIndex? _cartCatalog;
    private int _cartRevision = -1;
    private (bool Mode, long Supply, long BlackMarket, int Slots)? _cartTotals;

    private void AddToCart(CartKey key)
    {
        if (!TryGetEntry(key, out var entry))
            return;

        if (!CanAdd(key, entry))
            return;

        var cart = GetCurrentCart();
        cart[key] = GetCartAmount(key) + 1;
        if (_window != null)
            _window.BuyButton.IsConfirming = false;
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private void RemoveFromCart(CartKey key)
    {
        if (_submittingRequest)
            return;

        var cart = GetCurrentCart();
        var amount = GetCartAmount(key);
        if (amount <= 0)
            return;

        if (amount == 1)
            cart.Remove(key);
        else
            cart[key] = amount - 1;

        if (_window != null)
            _window.BuyButton.IsConfirming = false;
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private bool CanAdd(CartKey key, RequisitionsEntry entry)
    {
        if (_state == null || _submittingRequest || GetCartAmount(key) >= _state.MaxRequestItems)
            return false;
        var total = GetCartSupplyTotal() + entry.Cost;
        if (RequestOnly)
            return !entry.BlackMarket && total <= int.MaxValue;
        if (entry.Unit && entry.Cost <= 0)
            return false;
        var blackMarketTotal = GetCartBlackMarketTotal() + entry.BlackMarketCost;
        return (!entry.BlackMarket || CanUseBlackMarket()) && total <= _state.Balance &&
               blackMarketTotal <= _state.BlackMarketBalance &&
               GetCartSlots(key, GetCartAmount(key) + 1) <= GetRemainingCapacity();
    }

    private void RefreshCart()
    {
        if (_window == null)
            return;

        CartKey? focused = null;
        var cursor = 0;
        var renderRows = _window.CartTab.Visible;
        var rebuildRows = renderRows && (_cartRowsBlackMarket != _blackMarketMode ||
            _cartCatalog != CurrentCatalog || _cartRevision != CurrentCatalog.Revision);
        if (rebuildRows)
        {
            if (_cartRowsBlackMarket == _blackMarketMode)
            {
                foreach (var (key, row) in _cartRows)
                {
                    if (!row.Quantity.LineEditControl.HasKeyboardFocus())
                        continue;
                    focused = key;
                    cursor = row.Quantity.LineEditControl.CursorPosition;
                }
            }

            _window.CartContainer.DisposeAllChildren();
            _cartRows.Clear();
            _cartRowsBlackMarket = _blackMarketMode;
            _cartCatalog = CurrentCatalog;
            _cartRevision = CurrentCatalog.Revision;
        }

        _shownCartRows.Clear();
        var position = 0;
        var cart = GetCurrentCart();
        var search = _window.SearchBar.Text.Trim();
        var scopedItems = 0;
        var visibleItems = 0;
        var items = new List<(CartKey Key, int Amount)>(cart.Count);
        foreach (var (key, amount) in cart)
        {
            items.Add((key, amount));
        }

        items.Sort((a, b) =>
        {
            var categoryComparison = a.Key.Category.CompareTo(b.Key.Category);
            return categoryComparison != 0
                ? categoryComparison
                : a.Key.Entry.CompareTo(b.Key.Entry);
        });

        foreach (var (key, amount) in items)
        {
            if (!_blackMarketMode && SelectedCategory != null && SelectedCategory != key.Category)
                continue;

            if (!TryGetEntry(key, out var entry))
                continue;

            scopedItems++;
            var text = GetCatalogText(key, entry);
            if (!MatchesSearch(text.Name, search))
                continue;

            visibleItems++;
            if (!renderRows)
                continue;
            _shownCartRows.Add(key);
            if (_cartRows.TryGetValue(key, out var existing))
            {
                if (existing.Quantity.Value != amount)
                    existing.Quantity.OverrideValue(amount);
                SetText(existing.Cost, GetLineCostText(entry, amount));
                existing.AddButton.Disabled = !CanAdd(key, entry);
                existing.RemoveButton.Disabled = _submittingRequest;
                existing.Quantity.LineEditControl.Editable = !_submittingRequest;
                existing.SetPositionInParent(position++);
                continue;
            }

            var display = GetDisplay(entry);
            var row = new RequisitionsCartRow
            {
                Cost = { Text = GetLineCostText(entry, amount) },
                Icon =
                {
                    Textures = display.IconTextures,
                    Modulate = display.IconModulate,
                },
            };
            SetText(row.ProductName, display.Name, color: Color.White);
            SetText(row.Description, display.Description);

            row.AddButton.OnPressed += _ => AddToCart(key);
            row.RemoveButton.OnPressed += _ => RemoveFromCart(key);
            row.Quantity.IsValid = value => value >= 0;
            row.Quantity.OverrideValue(amount);
            row.Quantity.ValueChanged += args =>
            {
                SetCartAmount(key, args.Value);
                if (row.Quantity.Value != GetCartAmount(key))
                    row.Quantity.OverrideValue(GetCartAmount(key));
            };
            row.AddButton.Disabled = !CanAdd(key, entry);
            row.RemoveButton.Disabled = _submittingRequest;
            row.Quantity.LineEditControl.Editable = !_submittingRequest;

            _window.CartContainer.AddChild(row);
            _cartRows[key] = row;
            row.SetPositionInParent(position++);
            if (focused == key)
            {
                row.Quantity.LineEditControl.GrabKeyboardFocus();
                row.Quantity.LineEditControl.CursorPosition =
                    Math.Min(cursor, row.Quantity.LineEditControl.Text.Length);
            }
        }

        if (renderRows)
        {
            foreach (var key in _cartRows.Keys.Where(key => !_shownCartRows.Contains(key)).ToArray())
            {
                _cartRows[key].Dispose();
                _cartRows.Remove(key);
            }
        }

        var supplyTotal = GetCartSupplyTotal();
        var blackMarketTotal = GetCartBlackMarketTotal();
        var cartAmount = GetCartSlots();
        var remainingCapacity = GetRemainingCapacity();

        var totals = (_blackMarketMode, supplyTotal, blackMarketTotal, cartAmount);
        if (_cartTotals != totals)
            _window.BuyButton.IsConfirming = false;
        _cartTotals = totals;
        SetText(_window.CartTotalLabel, GetCartTotalText(supplyTotal, blackMarketTotal), markup: true);

        var status = string.Empty;
        if (cart.Count == 0)
            status = Loc.GetString("rmc-requisitions-cart-empty");
        else if (_blackMarketMode && !CanUseBlackMarket())
            status = GetBlackMarketStatusText();
        else if (visibleItems == 0 && !string.IsNullOrWhiteSpace(search))
            status = Loc.GetString("rmc-requisitions-cart-filter-empty");
        else if (scopedItems == 0)
            status = Loc.GetString("rmc-requisitions-cart-category-empty");
        else if (RequestOnly && string.IsNullOrWhiteSpace(_window.RequestReason.Text))
            status = Loc.GetString("rmc-requisitions-request-reason-required");
        else if (RequestOnly && supplyTotal > int.MaxValue)
            status = Loc.GetString("rmc-requisitions-request-invalid-items");
        else if (!RequestOnly && _state != null && supplyTotal > _state.Balance)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-funds");
        else if (_state != null && blackMarketTotal > _state.BlackMarketBalance)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-wy");
        else if (!RequestOnly && cartAmount > remainingCapacity)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-capacity");

        SetText(_window.CartStatusLabel, status);

        _window.ClearCartButton.Disabled = cart.Count == 0 || _submittingRequest;
        _window.RequestReason.Editable = !_submittingRequest;
        _window.BuyButton.Disabled = cart.Count == 0 ||
                                     _state == null ||
                                     _submittingRequest ||
                                     (RequestOnly && (string.IsNullOrWhiteSpace(_window.RequestReason.Text) ||
                                      supplyTotal > int.MaxValue)) ||
                                     (_blackMarketMode && !CanUseBlackMarket()) ||
                                     (!RequestOnly && (supplyTotal > _state.Balance ||
                                      blackMarketTotal > _state.BlackMarketBalance ||
                                      cartAmount > remainingCapacity));
    }

    private int GetCartAmount(CartKey key)
    {
        return GetCurrentCart().GetValueOrDefault(key);
    }

    private int GetCartAmount()
    {
        var amount = 0;
        foreach (var count in GetCurrentCart().Values)
        {
            amount += count;
        }

        return amount;
    }

    private long GetCartSupplyTotal()
    {
        var total = 0L;
        foreach (var (key, amount) in GetCurrentCart())
        {
            if (TryGetEntry(key, out var entry))
                total += (long) entry.Cost * amount;
        }

        return total;
    }

    private long GetCartBlackMarketTotal()
    {
        var total = 0L;
        foreach (var (key, amount) in GetCurrentCart())
        {
            if (TryGetEntry(key, out var entry))
                total += (long) entry.BlackMarketCost * amount;
        }

        return total;
    }
}
